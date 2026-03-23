using VideoCallApp.Application.Common;
using VideoCallApp.Application.DTOs.Meeting;

namespace VideoCallApp.Application.Interfaces;

public interface IMeetingService
{
    Task<ApiResponse<MeetingResponseDto>> CreateMeetingAsync(CreateMeetingRequestDto request, string userId);
    Task<ApiResponse<MeetingResponseDto>> GetMeetingByCodeAsync(string meetingCode);
    Task<ApiResponse<MeetingResponseDto>> GetMeetingByIdAsync(int meetingId);
    Task<ApiResponse<List<MeetingResponseDto>>> GetUserMeetingsAsync(string userId);
    Task<ApiResponse<MeetingResponseDto>> JoinMeetingAsync(string meetingCode, string userId, string connectionId);
    Task<ApiResponse<bool>> LeaveMeetingAsync(int meetingId, string userId);
    Task<ApiResponse<bool>> EndMeetingAsync(int meetingId, string userId);
    Task<ApiResponse<List<ParticipantDto>>> GetMeetingParticipantsAsync(int meetingId);
    Task<ApiResponse<bool>> UpdateParticipantMediaStateAsync(int meetingId, string userId, UpdateMediaStateDto request);
}