namespace VideoCallApp.Domain.Entities;

public class Meeting
{
    public int Id { get; set; }
    public string MeetingCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string HostId { get; set; } = string.Empty;
    public DateTime? ActualStartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsActive { get; set; } = true;
    public MeetingStatus Status { get; set; } = MeetingStatus.Scheduled;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual User Host { get; set; } = null!;
    public virtual ICollection<MeetingParticipant> Participants { get; set; } = new List<MeetingParticipant>();
    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}

public enum MeetingStatus
{
    Scheduled = 0,
    InProgress = 1,
    Ended = 2,
    Cancelled = 3
}
