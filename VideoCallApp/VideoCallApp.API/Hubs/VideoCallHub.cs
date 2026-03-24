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

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinMeeting(string meetingCode)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return;

        var result = await _meetingService.JoinMeetingAsync(meetingCode, userId, Context.ConnectionId);
        if (!result.Success || result.Data == null) return;

        var meetingId = result.Data.Id;
        ConnectionMeetings[Context.ConnectionId] = meetingId;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"meeting_{meetingId}");

        var participantsResult = await _meetingService.GetMeetingParticipantsAsync(meetingId);
        if (!participantsResult.Success || participantsResult.Data == null) return;

        var participantsWithHandState = ApplyRaisedHandState(meetingId, participantsResult.Data);
        var participant = participantsWithHandState.FirstOrDefault(p => p.UserId == userId);

        await Clients.OthersInGroup($"meeting_{meetingId}")
            .SendAsync("UserJoined", participant);

        await Clients.Caller.SendAsync("CurrentParticipants", participantsWithHandState);

        var currentScreenSharer = participantsWithHandState.FirstOrDefault(p => p.IsScreenSharing);
        await Clients.Caller.SendAsync("ScreenShareState", new
        {
            MeetingId = meetingId,
            UserId = currentScreenSharer?.UserId,
            IsSharing = currentScreenSharer != null
        });
    }

    public async Task LeaveMeeting(int meetingId)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId)) return;

        ConnectionMeetings.TryRemove(Context.ConnectionId, out _);
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
