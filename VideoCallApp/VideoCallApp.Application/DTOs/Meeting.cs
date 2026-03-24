namespace VideoCallApp.Application.DTOs.Meeting;

public class CreateMeetingRequestDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
}

public class MeetingResponseDto
{
    public int Id { get; set; }
    public string MeetingCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string HostId { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public DateTime? ActualStartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsActive { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CurrentParticipants { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class JoinMeetingRequestDto
{
    public string MeetingCode { get; set; } = string.Empty;
}

public class ParticipantDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public bool IsCameraOn { get; set; }
    public bool IsMicrophoneOn { get; set; }
    public bool IsScreenSharing { get; set; }
    public bool IsHandRaised { get; set; }
    public DateTime? HandRaisedAt { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public string? ConnectionId { get; set; }
}

public class UpdateMediaStateDto
{
    public bool? IsCameraOn { get; set; }
    public bool? IsMicrophoneOn { get; set; }
    public bool? IsScreenSharing { get; set; }
}
