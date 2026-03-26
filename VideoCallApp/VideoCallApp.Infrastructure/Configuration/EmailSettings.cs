namespace VideoCallApp.Infrastructure.Configuration;

public class EmailSettings
{
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Video Meet";
    public bool UseStartTls { get; set; } = true;
}
