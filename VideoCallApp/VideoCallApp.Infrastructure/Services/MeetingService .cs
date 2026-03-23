using AutoMapper;
using Microsoft.EntityFrameworkCore;
using VideoCallApp.Application.Common;
using VideoCallApp.Application.DTOs.Meeting;
using VideoCallApp.Application.Interfaces;
using VideoCallApp.Domain.Entities;
using VideoCallApp.Persistence.Data;

namespace VideoCallApp.Infrastructure.Services;

public class MeetingService : IMeetingService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public MeetingService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<MeetingResponseDto>> CreateMeetingAsync(
        CreateMeetingRequestDto request,
        string userId)
    {
        // Generate unique meeting code
        var meetingCode = GenerateMeetingCode();
        while (await _context.Meetings.AnyAsync(m => m.MeetingCode == meetingCode))
        {
            meetingCode = GenerateMeetingCode();
        }

        var meeting = _mapper.Map<Meeting>(request);
        meeting.MeetingCode = meetingCode;
        meeting.HostId = userId;
        meeting.Status = MeetingStatus.Scheduled;
        meeting.Title = string.IsNullOrWhiteSpace(meeting.Title) ? "Instant meeting" : meeting.Title.Trim();
        meeting.Description = string.IsNullOrWhiteSpace(meeting.Description) ? null : meeting.Description.Trim();
        meeting.CreatedAt = DateTime.UtcNow;

        _context.Meetings.Add(meeting);
        await _context.SaveChangesAsync();

        // Load host information
        await _context.Entry(meeting)
            .Reference(m => m.Host)
            .LoadAsync();

        var response = _mapper.Map<MeetingResponseDto>(meeting);

        return ApiResponse<MeetingResponseDto>.SuccessResponse(
            response,
            "Meeting created successfully");
    }

    public async Task<ApiResponse<MeetingResponseDto>> GetMeetingByCodeAsync(string meetingCode)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Host)
            .Include(m => m.Participants.Where(p => p.LeftAt == null))
            .FirstOrDefaultAsync(m => m.MeetingCode == meetingCode);

        if (meeting == null)
        {
            return ApiResponse<MeetingResponseDto>.ErrorResponse(
                "Meeting not found",
                "No meeting found with this code");
        }

        var response = _mapper.Map<MeetingResponseDto>(meeting);

        return ApiResponse<MeetingResponseDto>.SuccessResponse(response);
    }

    public async Task<ApiResponse<MeetingResponseDto>> GetMeetingByIdAsync(int meetingId)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Host)
            .Include(m => m.Participants.Where(p => p.LeftAt == null))
            .FirstOrDefaultAsync(m => m.Id == meetingId);

        if (meeting == null)
        {
            return ApiResponse<MeetingResponseDto>.ErrorResponse(
                "Meeting not found",
                "No meeting found with this ID");
        }

        var response = _mapper.Map<MeetingResponseDto>(meeting);

        return ApiResponse<MeetingResponseDto>.SuccessResponse(response);
    }

    public async Task<ApiResponse<List<MeetingResponseDto>>> GetUserMeetingsAsync(string userId)
    {
        var meetings = await _context.Meetings
            .Include(m => m.Host)
            .Include(m => m.Participants.Where(p => p.LeftAt == null))
            .Where(m => m.HostId == userId || m.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        var response = _mapper.Map<List<MeetingResponseDto>>(meetings);

        return ApiResponse<List<MeetingResponseDto>>.SuccessResponse(response);
    }

    public async Task<ApiResponse<MeetingResponseDto>> JoinMeetingAsync(
        string meetingCode,
        string userId,
        string connectionId)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Host)
            .Include(m => m.Participants)
            .FirstOrDefaultAsync(m => m.MeetingCode == meetingCode);

        if (meeting == null)
        {
            return ApiResponse<MeetingResponseDto>.ErrorResponse(
                "Meeting not found",
                "No meeting found with this code");
        }

        if (!meeting.IsActive)
        {
            return ApiResponse<MeetingResponseDto>.ErrorResponse(
                "Meeting inactive",
                "This meeting is no longer active");
        }

        // Check if user is already a participant
        var existingParticipant = meeting.Participants
            .FirstOrDefault(p => p.UserId == userId && p.LeftAt == null);

        if (existingParticipant != null)
        {
            // Update connection ID
            existingParticipant.ConnectionId = connectionId;
        }
        else
        {
            // Add new participant
            var participant = new MeetingParticipant
            {
                MeetingId = meeting.Id,
                UserId = userId,
                JoinedAt = DateTime.UtcNow,
                Role = meeting.HostId == userId ? ParticipantRole.Host : ParticipantRole.Participant,
                ConnectionId = connectionId,
                IsCameraOn = true,
                IsMicrophoneOn = true,
                IsScreenSharing = false
            };

            _context.MeetingParticipants.Add(participant);
        }

        // Update meeting status if first participant joining
        if (meeting.Status == MeetingStatus.Scheduled)
        {
            meeting.Status = MeetingStatus.InProgress;
            meeting.ActualStartTime = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Reload to get updated participant count
        await _context.Entry(meeting)
            .Collection(m => m.Participants)
            .Query()
            .Where(p => p.LeftAt == null)
            .LoadAsync();

        var response = _mapper.Map<MeetingResponseDto>(meeting);

        return ApiResponse<MeetingResponseDto>.SuccessResponse(
            response,
            "Joined meeting successfully");
    }

    public async Task<ApiResponse<bool>> LeaveMeetingAsync(int meetingId, string userId)
    {
        var participant = await _context.MeetingParticipants
            .FirstOrDefaultAsync(p => p.MeetingId == meetingId && p.UserId == userId && p.LeftAt == null);

        if (participant == null)
        {
            return ApiResponse<bool>.ErrorResponse(
                "Participant not found",
                "You are not in this meeting");
        }

        participant.LeftAt = DateTime.UtcNow;
        participant.ConnectionId = null;
        participant.IsScreenSharing = false;

        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(
            true,
            "Left meeting successfully");
    }

    public async Task<ApiResponse<bool>> EndMeetingAsync(int meetingId, string userId)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Participants.Where(p => p.LeftAt == null))
            .FirstOrDefaultAsync(m => m.Id == meetingId);

        if (meeting == null)
        {
            return ApiResponse<bool>.ErrorResponse(
                "Meeting not found",
                "No meeting found with this ID");
        }

        if (meeting.HostId != userId)
        {
            return ApiResponse<bool>.ErrorResponse(
                "Unauthorized",
                "Only the host can end the meeting");
        }

        meeting.Status = MeetingStatus.Ended;
        meeting.EndTime = DateTime.UtcNow;
        meeting.IsActive = false;

        // Mark all participants as left
        foreach (var participant in meeting.Participants)
        {
            participant.LeftAt = DateTime.UtcNow;
            participant.ConnectionId = null;
        }

        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(
            true,
            "Meeting ended successfully");
    }

    public async Task<ApiResponse<List<ParticipantDto>>> GetMeetingParticipantsAsync(int meetingId)
    {
        var participants = await _context.MeetingParticipants
            .Include(p => p.User)
            .Where(p => p.MeetingId == meetingId && p.LeftAt == null)
            .ToListAsync();

        var response = _mapper.Map<List<ParticipantDto>>(participants);

        return ApiResponse<List<ParticipantDto>>.SuccessResponse(response);
    }

    public async Task<ApiResponse<bool>> UpdateParticipantMediaStateAsync(
        int meetingId,
        string userId,
        UpdateMediaStateDto request)
    {
        var participant = await _context.MeetingParticipants
            .FirstOrDefaultAsync(p => p.MeetingId == meetingId && p.UserId == userId && p.LeftAt == null);

        if (participant == null)
        {
            return ApiResponse<bool>.ErrorResponse(
                "Participant not found",
                "You are not in this meeting");
        }

        if (request.IsCameraOn.HasValue)
            participant.IsCameraOn = request.IsCameraOn.Value;

        if (request.IsMicrophoneOn.HasValue)
            participant.IsMicrophoneOn = request.IsMicrophoneOn.Value;

        if (request.IsScreenSharing.HasValue)
        {
            if (request.IsScreenSharing.Value)
            {
                var otherActiveSharers = await _context.MeetingParticipants
                    .Where(p => p.MeetingId == meetingId && p.UserId != userId && p.LeftAt == null && p.IsScreenSharing)
                    .ToListAsync();

                foreach (var otherParticipant in otherActiveSharers)
                {
                    otherParticipant.IsScreenSharing = false;
                }
            }

            participant.IsScreenSharing = request.IsScreenSharing.Value;
        }

        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(
            true,
            "Media state updated successfully");
    }

    private string GenerateMeetingCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
