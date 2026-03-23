using AutoMapper;
using Microsoft.EntityFrameworkCore;
using VideoCallApp.Application.Common;
using VideoCallApp.Application.DTOs.Chat;
using VideoCallApp.Application.Interfaces;
using VideoCallApp.Domain.Entities;
using VideoCallApp.Persistence.Data;

namespace VideoCallApp.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public ChatService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ChatMessageDto>> SendMessageAsync(
        int meetingId,
        string userId,
        SendMessageDto message)
    {
        // Verify user is in meeting
        var participant = await _context.MeetingParticipants
            .AnyAsync(p => p.MeetingId == meetingId && p.UserId == userId && p.LeftAt == null);

        if (!participant)
        {
            return ApiResponse<ChatMessageDto>.ErrorResponse(
                "Unauthorized",
                "You are not a participant in this meeting");
        }

        var chatMessage = new ChatMessage
        {
            MeetingId = meetingId,
            SenderId = userId,
            Message = message.Message,
            SentAt = DateTime.UtcNow,
            Type = MessageType.Text
        };

        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();

        // Load sender information
        await _context.Entry(chatMessage)
            .Reference(c => c.Sender)
            .LoadAsync();

        var response = _mapper.Map<ChatMessageDto>(chatMessage);

        return ApiResponse<ChatMessageDto>.SuccessResponse(
            response,
            "Message sent successfully");
    }

    public async Task<ApiResponse<List<ChatMessageDto>>> GetMeetingMessagesAsync(int meetingId)
    {
        var messages = await _context.ChatMessages
            .Include(c => c.Sender)
            .Where(c => c.MeetingId == meetingId)
            .OrderBy(c => c.SentAt)
            .ToListAsync();

        var response = _mapper.Map<List<ChatMessageDto>>(messages);

        return ApiResponse<List<ChatMessageDto>>.SuccessResponse(response);
    }
}