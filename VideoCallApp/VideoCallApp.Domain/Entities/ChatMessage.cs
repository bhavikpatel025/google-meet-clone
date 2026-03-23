namespace VideoCallApp.Domain.Entities;

public class ChatMessage
{
    public int Id { get; set; }
    public int MeetingId { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public MessageType Type { get; set; } = MessageType.Text;

    // Navigation properties
    public virtual Meeting Meeting { get; set; } = null!;
    public virtual User Sender { get; set; } = null!;
}

public enum MessageType
{
    Text = 0,
    System = 1
}