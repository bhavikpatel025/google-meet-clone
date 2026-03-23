using VideoCallApp.Application.Common;
using VideoCallApp.Application.DTOs.Chat;

namespace VideoCallApp.Application.Interfaces;

public interface IChatService
{
    Task<ApiResponse<ChatMessageDto>> SendMessageAsync(int meetingId, string userId, SendMessageDto message);
    Task<ApiResponse<List<ChatMessageDto>>> GetMeetingMessagesAsync(int meetingId);
}