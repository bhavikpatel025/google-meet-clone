using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VideoCallApp.Application.DTOs.Meeting;
using VideoCallApp.Application.Interfaces;

namespace VideoCallApp.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MeetingController : ControllerBase
{
    private readonly IMeetingService _meetingService;
    private readonly IValidator<CreateMeetingRequestDto> _createMeetingValidator;
    private readonly IValidator<JoinMeetingRequestDto> _joinMeetingValidator;

    public MeetingController(
        IMeetingService meetingService,
        IValidator<CreateMeetingRequestDto> createMeetingValidator,
        IValidator<JoinMeetingRequestDto> joinMeetingValidator)
    {
        _meetingService = meetingService;
        _createMeetingValidator = createMeetingValidator;
        _joinMeetingValidator = joinMeetingValidator;
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    /// <summary>
    /// Create a new meeting
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateMeeting([FromBody] CreateMeetingRequestDto? request)
    {
        request ??= new CreateMeetingRequestDto();

        var validationResult = await _createMeetingValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errors = validationResult.Errors.Select(e => e.ErrorMessage)
            });
        }

        var userId = GetUserId();
        var result = await _meetingService.CreateMeetingAsync(request, userId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get meeting by code
    /// </summary>
    [HttpGet("code/{meetingCode}")]
    public async Task<IActionResult> GetMeetingByCode(string meetingCode)
    {
        var result = await _meetingService.GetMeetingByCodeAsync(meetingCode);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get meeting by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetMeetingById(int id)
    {
        var result = await _meetingService.GetMeetingByIdAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get user's meetings
    /// </summary>
    [HttpGet("my-meetings")]
    public async Task<IActionResult> GetMyMeetings()
    {
        var userId = GetUserId();
        var result = await _meetingService.GetUserMeetingsAsync(userId);

        return Ok(result);
    }

    /// <summary>
    /// Join meeting via HTTP (alternative to SignalR)
    /// </summary>
    [HttpPost("join")]
    public async Task<IActionResult> JoinMeeting([FromBody] JoinMeetingRequestDto request)
    {
        var validationResult = await _joinMeetingValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errors = validationResult.Errors.Select(e => e.ErrorMessage)
            });
        }

        var userId = GetUserId();
        var result = await _meetingService.JoinMeetingAsync(request.MeetingCode, userId, "http");

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Leave meeting
    /// </summary>
    [HttpPost("{id}/leave")]
    public async Task<IActionResult> LeaveMeeting(int id)
    {
        var userId = GetUserId();
        var result = await _meetingService.LeaveMeetingAsync(id, userId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// End meeting (host only)
    /// </summary>
    [HttpPost("{id}/end")]
    public async Task<IActionResult> EndMeeting(int id)
    {
        var userId = GetUserId();
        var result = await _meetingService.EndMeetingAsync(id, userId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get meeting participants
    /// </summary>
    [HttpGet("{id}/participants")]
    public async Task<IActionResult> GetParticipants(int id)
    {
        var result = await _meetingService.GetMeetingParticipantsAsync(id);

        return Ok(result);
    }

    /// <summary>
    /// Update participant media state
    /// </summary>
    [HttpPut("{id}/media-state")]
    public async Task<IActionResult> UpdateMediaState(int id, [FromBody] UpdateMediaStateDto request)
    {
        var userId = GetUserId();
        var result = await _meetingService.UpdateParticipantMediaStateAsync(id, userId, request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
