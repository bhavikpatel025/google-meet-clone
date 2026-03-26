using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;
using VideoCallApp.Application.DTOs.Chat;
using VideoCallApp.Application.DTOs.Meeting;
using VideoCallApp.Application.Interfaces;

namespace VideoCallApp.API.Hubs;

[Authorize]
public class VideoCallHub : Hub
{
    private sealed class WaitingParticipantState
    {
        public required string UserId { get; init; }
        public required string UserName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public required string ConnectionId { get; set; }
        public required DateTime RequestedAtUtc { get; init; }
    }

    private sealed class HandRaiseState
    {
        public required string UserId { get; init; }
        public required string UserName { get; init; }
        public required DateTime RaisedAtUtc { get; init; }
    }

    private readonly IMeetingService _meetingService;
    private readonly IChatService _chatService;

    private static readonly ConcurrentDictionary<string, int> ConnectionMeetings = new();
    private static readonly ConcurrentDictionary<int, List<HandRaiseState>> MeetingRaisedHands = new();
    private static readonly ConcurrentDictionary<int, List<WaitingParticipantState>> MeetingWaitingParticipants = new();

    public VideoCallHub(IMeetingService meetingService, IChatService chatService)
    {
        _meetingService = meetingService;
        _chatService = chatService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        if (ConnectionMeetings.TryRemove(Context.ConnectionId, out var meetingId) && !string.IsNullOrWhiteSpace(userId))
        {
            RemoveRaisedHand(meetingId, userId);
            await _meetingService.LeaveMeetingAsync(meetingId, userId);

            await Clients.Group($"meeting_{meetingId}")
                .SendAsync("UserLeft", new { UserId = userId });

            var participantsResult = await _meetingService.GetMeetingParticipantsAsync(meetingId);
            if (participantsResult.Success && participantsResult.Data != null)
            {
                await BroadcastMeetingState(meetingId, participantsResult.Data);
            }
        }
        else if (!string.IsNullOrWhiteSpace(userId))
        {
            var waitingMeetingId = RemoveWaitingParticipantByConnection(Context.ConnectionId, userId);
            if (waitingMeetingId.HasValue)
            {
                await BroadcastWaitingParticipants(waitingMeetingId.Value);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinMeeting(string meetingCode, string? userName = null, string? profilePictureUrl = null)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return;

        var meetingResult = await _meetingService.GetMeetingByCodeAsync(meetingCode);
        if (!meetingResult.Success || meetingResult.Data == null) return;

        var meeting = meetingResult.Data;
        var meetingId = meeting.Id;
        var participantsResult = await _meetingService.GetMeetingParticipantsAsync(meetingId);
        if (!participantsResult.Success || participantsResult.Data == null) return;

        var alreadyJoined = participantsResult.Data.Any(participant => participant.UserId == userId);
        var shouldJoinDirectly = meeting.HostId == userId || alreadyJoined;

        if (!shouldJoinDirectly)
        {
            var resolvedUserName = string.IsNullOrWhiteSpace(userName)
                ? Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Guest"
                : userName;

            AddOrUpdateWaitingParticipant(meetingId, new WaitingParticipantState
            {
                UserId = userId,
                UserName = resolvedUserName,
                ProfilePictureUrl = profilePictureUrl,
                ConnectionId = Context.ConnectionId,
                RequestedAtUtc = DateTime.UtcNow
            });

            await Clients.Caller.SendAsync("WaitingRoomEntered", new
            {
                MeetingId = meetingId,
                IsWaiting = true
            });

            await BroadcastWaitingParticipants(meetingId);
            return;
        }

        await JoinApprovedParticipant(meetingCode, meetingId, userId, Context.ConnectionId, sendUserJoinedEvent: !alreadyJoined);
    }

    public async Task LeaveMeeting(int meetingId)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return;

        if (!ConnectionMeetings.TryRemove(Context.ConnectionId, out _))
        {
            var waitingMeetingId = RemoveWaitingParticipantByConnection(Context.ConnectionId, userId);
            if (waitingMeetingId.HasValue)
            {
                await Clients.Caller.SendAsync("WaitingRoomEntered", new
                {
                    MeetingId = waitingMeetingId.Value,
                    IsWaiting = false
                });
                await BroadcastWaitingParticipants(waitingMeetingId.Value);
            }

            return;
        }

        RemoveRaisedHand(meetingId, userId);

        await _meetingService.LeaveMeetingAsync(meetingId, userId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"meeting_{meetingId}");

        await Clients.Group($"meeting_{meetingId}")
            .SendAsync("UserLeft", new { UserId = userId });

        var participantsResult = await _meetingService.GetMeetingParticipantsAsync(meetingId);
        if (participantsResult.Success && participantsResult.Data != null)
        {
            await BroadcastMeetingState(meetingId, participantsResult.Data);
        }
    }

    public async Task AdmitParticipant(int meetingId, string targetUserId)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(targetUserId)) return;

        var meetingResult = await _meetingService.GetMeetingByIdAsync(meetingId);
        if (!meetingResult.Success || meetingResult.Data == null || meetingResult.Data.HostId != userId) return;

        var waitingParticipant = RemoveWaitingParticipant(meetingId, targetUserId);
        if (waitingParticipant == null) return;

        await JoinApprovedParticipant(
            meetingResult.Data.MeetingCode,
            meetingId,
            waitingParticipant.UserId,
            waitingParticipant.ConnectionId,
            sendUserJoinedEvent: true);
    }

    public async Task AdmitAllParticipants(int meetingId)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return;

        var meetingResult = await _meetingService.GetMeetingByIdAsync(meetingId);
        if (!meetingResult.Success || meetingResult.Data == null || meetingResult.Data.HostId != userId) return;

        List<WaitingParticipantState> waitingParticipants;
        if (!MeetingWaitingParticipants.TryGetValue(meetingId, out var waitingList))
        {
            return;
        }

        lock (waitingList)
        {
            waitingParticipants = waitingList
                .Select(entry => new WaitingParticipantState
                {
                    UserId = entry.UserId,
                    UserName = entry.UserName,
                    ProfilePictureUrl = entry.ProfilePictureUrl,
                    ConnectionId = entry.ConnectionId,
                    RequestedAtUtc = entry.RequestedAtUtc
                })
                .ToList();
        }

        foreach (var waitingParticipant in waitingParticipants)
        {
            RemoveWaitingParticipant(meetingId, waitingParticipant.UserId);
            await JoinApprovedParticipant(
                meetingResult.Data.MeetingCode,
                meetingId,
                waitingParticipant.UserId,
                waitingParticipant.ConnectionId,
                sendUserJoinedEvent: true);
        }
    }

    public async Task DenyParticipant(int meetingId, string targetUserId)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(targetUserId)) return;

        var meetingResult = await _meetingService.GetMeetingByIdAsync(meetingId);
        if (!meetingResult.Success || meetingResult.Data == null || meetingResult.Data.HostId != userId) return;

        var waitingParticipant = RemoveWaitingParticipant(meetingId, targetUserId);
        if (waitingParticipant == null) return;

        await Clients.Client(waitingParticipant.ConnectionId)
            .SendAsync("JoinDenied", new
            {
                MeetingId = meetingId
            });

        await BroadcastWaitingParticipants(meetingId);
    }

    public async Task SendOffer(int meetingId, string targetUserId, string offer)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return;

        await Clients.Group($"user_{targetUserId}")
            .SendAsync("ReceiveOffer", new { FromUserId = userId, Offer = offer });
    }

    public async Task SendAnswer(int meetingId, string targetUserId, string answer)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return;

        await Clients.Group($"user_{targetUserId}")
            .SendAsync("ReceiveAnswer", new { FromUserId = userId, Answer = answer });
    }

    public async Task SendIceCandidate(int meetingId, string targetUserId, string candidate)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return;

        await Clients.Group($"user_{targetUserId}")
            .SendAsync("ReceiveIceCandidate", new { FromUserId = userId, Candidate = candidate });
    }

    public async Task ToggleCamera(int meetingId, bool isOn)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return;

        await _meetingService.UpdateParticipantMediaStateAsync(
            meetingId,
            userId,
            new UpdateMediaStateDto { IsCameraOn = isOn });

        await Clients.Group($"meeting_{meetingId}")
            .SendAsync("CameraToggled", new { UserId = userId, IsOn = isOn });
    }

    public async Task ToggleMicrophone(int meetingId, bool isOn)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return;

        await _meetingService.UpdateParticipantMediaStateAsync(
            meetingId,
            userId,
            new UpdateMediaStateDto { IsMicrophoneOn = isOn });

        await Clients.Group($"meeting_{meetingId}")
            .SendAsync("MicrophoneToggled", new { UserId = userId, IsOn = isOn });
    }

    public async Task ToggleScreenShare(int meetingId, bool isSharing)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return;

        await _meetingService.UpdateParticipantMediaStateAsync(
            meetingId,
            userId,
            new UpdateMediaStateDto { IsScreenSharing = isSharing });

        var participantsResult = await _meetingService.GetMeetingParticipantsAsync(meetingId);
        if (!participantsResult.Success || participantsResult.Data == null) return;

        if (isSharing)
        {
            await Clients.Group($"meeting_{meetingId}")
                .SendAsync("ScreenShareStarted", new
                {
                    MeetingId = meetingId,
                    UserId = userId
                });
        }
        else
        {
            await Clients.Group($"meeting_{meetingId}")
                .SendAsync("ScreenShareStopped", new
                {
                    MeetingId = meetingId,
                    UserId = userId
                });
        }

        await BroadcastMeetingState(meetingId, participantsResult.Data);
    }

    public async Task SendMessage(int meetingId, string message)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return;

        var result = await _chatService.SendMessageAsync(
            meetingId,
            userId,
            new SendMessageDto { Message = message });

        if (result.Success && result.Data != null)
        {
            await Clients.Group($"meeting_{meetingId}")
                .SendAsync("NewMessage", result.Data);
        }
    }

    public async Task EndMeeting(int meetingId)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return;

        var result = await _meetingService.EndMeetingAsync(meetingId, userId);
        if (result.Success)
        {
            MeetingRaisedHands.TryRemove(meetingId, out _);
            MeetingWaitingParticipants.TryRemove(meetingId, out _);
            await Clients.Group($"meeting_{meetingId}")
                .SendAsync("MeetingEnded");
        }
    }

    public async Task RaiseHand(int meetingId)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return;

        var participantsResult = await _meetingService.GetMeetingParticipantsAsync(meetingId);
        if (!participantsResult.Success || participantsResult.Data == null) return;

        var participant = participantsResult.Data.FirstOrDefault(currentParticipant => currentParticipant.UserId == userId);
        if (participant == null) return;

        var raisedAt = AddRaisedHand(meetingId, userId, participant.UserName);

        await Clients.Group($"meeting_{meetingId}")
            .SendAsync("HandRaised", new
            {
                MeetingId = meetingId,
                UserId = userId,
                UserName = participant.UserName,
                HandRaisedAt = raisedAt
            });

        await BroadcastMeetingState(meetingId, participantsResult.Data);
    }

    public async Task LowerHand(int meetingId, string targetUserId)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(targetUserId)) return;

        var meetingResult = await _meetingService.GetMeetingByIdAsync(meetingId);
        if (!meetingResult.Success || meetingResult.Data == null) return;

        var canLower = targetUserId == userId || meetingResult.Data.HostId == userId;
        if (!canLower) return;

        var wasRaised = RemoveRaisedHand(meetingId, targetUserId);
        if (!wasRaised) return;

        await Clients.Group($"meeting_{meetingId}")
            .SendAsync("HandLowered", new
            {
                MeetingId = meetingId,
                UserId = targetUserId
            });

        var participantsResult = await _meetingService.GetMeetingParticipantsAsync(meetingId);
        if (participantsResult.Success && participantsResult.Data != null)
        {
            await BroadcastMeetingState(meetingId, participantsResult.Data);
        }
    }

    public async Task LowerAllHands(int meetingId)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return;

        var meetingResult = await _meetingService.GetMeetingByIdAsync(meetingId);
        if (!meetingResult.Success || meetingResult.Data == null || meetingResult.Data.HostId != userId) return;

        ClearRaisedHands(meetingId);

        await Clients.Group($"meeting_{meetingId}")
            .SendAsync("AllHandsLowered", new { MeetingId = meetingId });

        var participantsResult = await _meetingService.GetMeetingParticipantsAsync(meetingId);
        if (participantsResult.Success && participantsResult.Data != null)
        {
            await BroadcastMeetingState(meetingId, participantsResult.Data);
        }
    }

    public async Task SendReaction(int meetingId, string reaction)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(reaction)) return;

        var participantsResult = await _meetingService.GetMeetingParticipantsAsync(meetingId);
        if (!participantsResult.Success || participantsResult.Data == null) return;

        var participant = participantsResult.Data.FirstOrDefault(currentParticipant => currentParticipant.UserId == userId);
        if (participant == null) return;

        await Clients.Group($"meeting_{meetingId}")
            .SendAsync("ReactionSent", new
            {
                Id = Guid.NewGuid().ToString("N"),
                MeetingId = meetingId,
                UserId = userId,
                UserName = participant.UserName,
                Reaction = reaction,
                SentAt = DateTime.UtcNow
            });
    }

    private string? GetUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private async Task BroadcastMeetingState(int meetingId, IEnumerable<ParticipantDto> participants)
    {
        var participantList = ApplyRaisedHandState(meetingId, participants).ToList();
        var activeSharer = participantList.FirstOrDefault(p => p.IsScreenSharing);

        await Clients.Group($"meeting_{meetingId}")
            .SendAsync("CurrentParticipants", participantList);

        await Clients.Group($"meeting_{meetingId}")
            .SendAsync("ScreenShareState", new
            {
                MeetingId = meetingId,
                UserId = activeSharer?.UserId,
                IsSharing = activeSharer != null
            });
    }

    private async Task BroadcastWaitingParticipants(int meetingId)
    {
        await Clients.Group($"meeting_{meetingId}")
            .SendAsync("WaitingParticipantsUpdated", GetWaitingParticipantsSnapshot(meetingId));
    }

    private async Task JoinApprovedParticipant(
        string meetingCode,
        int meetingId,
        string userId,
        string connectionId,
        bool sendUserJoinedEvent)
    {
        var result = await _meetingService.JoinMeetingAsync(meetingCode, userId, connectionId);
        if (!result.Success || result.Data == null) return;

        ConnectionMeetings[connectionId] = meetingId;

        await Groups.AddToGroupAsync(connectionId, $"meeting_{meetingId}");
        await Clients.Client(connectionId).SendAsync("JoinApproved", new { MeetingId = meetingId });

        var participantsResult = await _meetingService.GetMeetingParticipantsAsync(meetingId);
        if (!participantsResult.Success || participantsResult.Data == null) return;

        var participantsWithHandState = ApplyRaisedHandState(meetingId, participantsResult.Data);
        var participant = participantsWithHandState.FirstOrDefault(currentParticipant => currentParticipant.UserId == userId);

        if (sendUserJoinedEvent && participant != null)
        {
            await Clients.OthersInGroup($"meeting_{meetingId}")
                .SendAsync("UserJoined", participant);
        }

        await Clients.Client(connectionId).SendAsync("CurrentParticipants", participantsWithHandState);

        var currentScreenSharer = participantsWithHandState.FirstOrDefault(currentParticipant => currentParticipant.IsScreenSharing);
        await Clients.Client(connectionId).SendAsync("ScreenShareState", new
        {
            MeetingId = meetingId,
            UserId = currentScreenSharer?.UserId,
            IsSharing = currentScreenSharer != null
        });

        await BroadcastWaitingParticipants(meetingId);
        await BroadcastMeetingState(meetingId, participantsResult.Data);
    }

    private DateTime AddRaisedHand(int meetingId, string userId, string userName)
    {
        var raisedHands = MeetingRaisedHands.GetOrAdd(meetingId, _ => new List<HandRaiseState>());

        lock (raisedHands)
        {
            var existing = raisedHands.FirstOrDefault(entry => entry.UserId == userId);
            if (existing != null)
            {
                return existing.RaisedAtUtc;
            }

            var raisedAt = DateTime.UtcNow;
            raisedHands.Add(new HandRaiseState
            {
                UserId = userId,
                UserName = userName,
                RaisedAtUtc = raisedAt
            });

            return raisedAt;
        }
    }

    private bool RemoveRaisedHand(int meetingId, string userId)
    {
        if (!MeetingRaisedHands.TryGetValue(meetingId, out var raisedHands))
        {
            return false;
        }

        lock (raisedHands)
        {
            var removed = raisedHands.RemoveAll(entry => entry.UserId == userId) > 0;
            if (raisedHands.Count == 0)
            {
                MeetingRaisedHands.TryRemove(meetingId, out _);
            }

            return removed;
        }
    }

    private void ClearRaisedHands(int meetingId)
    {
        MeetingRaisedHands.TryRemove(meetingId, out _);
    }

    private void AddOrUpdateWaitingParticipant(int meetingId, WaitingParticipantState participant)
    {
        var waitingParticipants = MeetingWaitingParticipants.GetOrAdd(meetingId, _ => new List<WaitingParticipantState>());

        lock (waitingParticipants)
        {
            var existing = waitingParticipants.FirstOrDefault(entry => entry.UserId == participant.UserId);
            if (existing != null)
            {
                existing.UserName = participant.UserName;
                existing.ProfilePictureUrl = participant.ProfilePictureUrl;
                existing.ConnectionId = participant.ConnectionId;
                return;
            }

            waitingParticipants.Add(participant);
        }
    }

    private WaitingParticipantState? RemoveWaitingParticipant(int meetingId, string userId)
    {
        if (!MeetingWaitingParticipants.TryGetValue(meetingId, out var waitingParticipants))
        {
            return null;
        }

        lock (waitingParticipants)
        {
            var existing = waitingParticipants.FirstOrDefault(entry => entry.UserId == userId);
            if (existing == null)
            {
                return null;
            }

            waitingParticipants.Remove(existing);
            if (waitingParticipants.Count == 0)
            {
                MeetingWaitingParticipants.TryRemove(meetingId, out _);
            }

            return new WaitingParticipantState
            {
                UserId = existing.UserId,
                UserName = existing.UserName,
                ProfilePictureUrl = existing.ProfilePictureUrl,
                ConnectionId = existing.ConnectionId,
                RequestedAtUtc = existing.RequestedAtUtc
            };
        }
    }

    private int? RemoveWaitingParticipantByConnection(string connectionId, string userId)
    {
        foreach (var entry in MeetingWaitingParticipants)
        {
            lock (entry.Value)
            {
                var waitingParticipant = entry.Value.FirstOrDefault(participant =>
                    participant.ConnectionId == connectionId || participant.UserId == userId);

                if (waitingParticipant == null)
                {
                    continue;
                }

                entry.Value.Remove(waitingParticipant);
                if (entry.Value.Count == 0)
                {
                    MeetingWaitingParticipants.TryRemove(entry.Key, out _);
                }

                return entry.Key;
            }
        }

        return null;
    }

    private List<object> GetWaitingParticipantsSnapshot(int meetingId)
    {
        if (!MeetingWaitingParticipants.TryGetValue(meetingId, out var waitingParticipants))
        {
            return new List<object>();
        }

        lock (waitingParticipants)
        {
            return waitingParticipants
                .OrderBy(participant => participant.RequestedAtUtc)
                .Select(participant => new
                {
                    participant.UserId,
                    participant.UserName,
                    participant.ProfilePictureUrl,
                    RequestedAt = participant.RequestedAtUtc
                })
                .Cast<object>()
                .ToList();
        }
    }

    private List<ParticipantDto> ApplyRaisedHandState(int meetingId, IEnumerable<ParticipantDto> participants)
    {
        if (!MeetingRaisedHands.TryGetValue(meetingId, out var raisedHands))
        {
            return participants.Select(CloneParticipant).ToList();
        }

        Dictionary<string, HandRaiseState> raisedHandLookup;
        lock (raisedHands)
        {
            raisedHandLookup = raisedHands.ToDictionary(entry => entry.UserId, entry => entry);
        }

        return participants
            .Select(participant =>
            {
                var clone = CloneParticipant(participant);
                if (raisedHandLookup.TryGetValue(participant.UserId, out var handRaiseState))
                {
                    clone.IsHandRaised = true;
                    clone.HandRaisedAt = handRaiseState.RaisedAtUtc;
                }

                return clone;
            })
            .OrderByDescending(participant => participant.IsScreenSharing)
            .ThenBy(participant => participant.HandRaisedAt ?? DateTime.MaxValue)
            .ThenBy(participant => participant.JoinedAt)
            .ToList();
    }

    private static ParticipantDto CloneParticipant(ParticipantDto participant)
    {
        return new ParticipantDto
        {
            Id = participant.Id,
            UserId = participant.UserId,
            UserName = participant.UserName,
            Email = participant.Email,
            ProfilePictureUrl = participant.ProfilePictureUrl,
            IsCameraOn = participant.IsCameraOn,
            IsMicrophoneOn = participant.IsMicrophoneOn,
            IsScreenSharing = participant.IsScreenSharing,
            IsHandRaised = participant.IsHandRaised,
            HandRaisedAt = participant.HandRaisedAt,
            Role = participant.Role,
            JoinedAt = participant.JoinedAt,
            ConnectionId = participant.ConnectionId
        };
    }
}
