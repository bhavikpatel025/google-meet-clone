using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
    private readonly IMeetingInvitationEmailService _meetingInvitationEmailService;
    private readonly IValidator<CreateMeetingRequestDto> _createMeetingValidator;
    private readonly IValidator<JoinMeetingRequestDto> _joinMeetingValidator;
    private readonly IValidator<InviteParticipantsRequestDto> _inviteParticipantsValidator;
    private readonly IConfiguration _configuration;

    public MeetingController(
        IMeetingService meetingService,
        IValidator<CreateMeetingRequestDto> createMeetingValidator,
        IValidator<JoinMeetingRequestDto> joinMeetingValidator,
        IValidator<InviteParticipantsRequestDto> inviteParticipantsValidator,
        IMeetingInvitationEmailService meetingInvitationEmailService,
        IConfiguration configuration)
    {
        _meetingService = meetingService;
        _createMeetingValidator = createMeetingValidator;
        _joinMeetingValidator = joinMeetingValidator;
        _inviteParticipantsValidator = inviteParticipantsValidator;
        _meetingInvitationEmailService = meetingInvitationEmailService;
        _configuration = configuration;
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
    /// Invite participants by email
    /// </summary>
    [HttpPost("invite")]
    public async Task<IActionResult> InviteParticipants([FromBody] InviteParticipantsRequestDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _inviteParticipantsValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errors = validationResult.Errors.Select(e => e.ErrorMessage)
            });
        }

        var userId = GetUserId();
        var meetingResult = await _meetingService.GetMeetingByIdAsync(request.MeetingId);
        if (!meetingResult.Success || meetingResult.Data == null)
        {
            return NotFound(new { success = false, message = "Meeting not found" });
        }

        if (meetingResult.Data.HostId != userId)
        {
            return Forbid();
        }

        var participantsResult = await _meetingService.GetMeetingParticipantsAsync(request.MeetingId);
        var participantEmails = participantsResult.Success && participantsResult.Data != null
            ? participantsResult.Data
                .Select(participant => participant.Email)
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var normalizedEmails = request.Emails
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var invitedEmails = normalizedEmails
            .Where(email => !participantEmails.Contains(email))
            .ToList();

        if (invitedEmails.Count == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "No new participants to invite"
            });
        }

        var skippedEmails = normalizedEmails
            .Where(email => !invitedEmails.Contains(email, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var clientBaseUrl = _configuration["ClientApp:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:4200";
        var meetingLink = $"{clientBaseUrl}/join/{meetingResult.Data.MeetingCode}";

        await _meetingInvitationEmailService.SendMeetingInvitationsAsync(
            invitedEmails,
            string.IsNullOrWhiteSpace(request.HostName) ? meetingResult.Data.HostName : request.HostName,
            meetingResult.Data.Title,
            meetingResult.Data.MeetingCode,
            meetingLink,
            cancellationToken);

        return Ok(new
        {
            success = true,
            message = "Invitations sent successfully",
            data = new InviteParticipantsResponseDto
            {
                SentCount = invitedEmails.Count,
                InvitedEmails = invitedEmails,
                SkippedEmails = skippedEmails
            }
        });
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
