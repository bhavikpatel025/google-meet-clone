namespace VideoCallApp.Application.DTOs.Chat;

public class SendMessageDto
{
    public string Message { get; set; } = string.Empty;
}

public class ChatMessageDto
{
    public int Id { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public string Type { get; set; } = string.Empty;
}