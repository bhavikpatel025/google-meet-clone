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
    private readonly IMeetingService _meetingService;
    private readonly IChatService _chatService;

    private static readonly ConcurrentDictionary<string, int> ConnectionMeetings = new();

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

        var participant = participantsResult.Data.FirstOrDefault(p => p.UserId == userId);

        await Clients.OthersInGroup($"meeting_{meetingId}")
            .SendAsync("UserJoined", participant);

        await Clients.Caller.SendAsync("CurrentParticipants", participantsResult.Data);

        var currentScreenSharer = participantsResult.Data.FirstOrDefault(p => p.IsScreenSharing);
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
            await Clients.Group($"meeting_{meetingId}")
                .SendAsync("MeetingEnded");
        }
    }

    private string? GetUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private async Task BroadcastMeetingState(int meetingId, IEnumerable<ParticipantDto> participants)
    {
        var participantList = participants.ToList();
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
}
