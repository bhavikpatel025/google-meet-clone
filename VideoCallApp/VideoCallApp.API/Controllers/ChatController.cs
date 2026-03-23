using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VideoCallApp.Application.DTOs.Chat;
using VideoCallApp.Application.Interfaces;

namespace VideoCallApp.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    /// <summary>
    /// Send a message to meeting chat
    /// </summary>
    [HttpPost("meeting/{meetingId}")]
    public async Task<IActionResult> SendMessage(int meetingId, [FromBody] SendMessageDto message)
    {
        var userId = GetUserId();
        var result = await _chatService.SendMessageAsync(meetingId, userId, message);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all messages for a meeting
    /// </summary>
    [HttpGet("meeting/{meetingId}")]
    public async Task<IActionResult> GetMessages(int meetingId)
    {
        var result = await _chatService.GetMeetingMessagesAsync(meetingId);

        return Ok(result);
    }
}