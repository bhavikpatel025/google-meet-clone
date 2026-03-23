namespace VideoCallApp.Domain.Entities;

public class MeetingParticipant
{
    public int Id { get; set; }
    public int MeetingId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public bool IsCameraOn { get; set; } = true;
    public bool IsMicrophoneOn { get; set; } = true;
    public bool IsScreenSharing { get; set; } = false;
    public ParticipantRole Role { get; set; } = ParticipantRole.Participant;
    public string? ConnectionId { get; set; }

    // Navigation properties
    public virtual Meeting Meeting { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

public enum ParticipantRole
{
    Host = 0,
    Participant = 1
}