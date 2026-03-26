using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net;
using VideoCallApp.Application.Interfaces;
using VideoCallApp.Infrastructure.Configuration;

namespace VideoCallApp.Infrastructure.Services;

public class MeetingInvitationEmailService : IMeetingInvitationEmailService
{
    private readonly EmailSettings _emailSettings;

    public MeetingInvitationEmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task SendMeetingInvitationsAsync(
        IReadOnlyCollection<string> recipientEmails,
        string hostName,
        string meetingTitle,
        string meetingCode,
        string meetingLink,
        CancellationToken cancellationToken = default)
    {
        if (recipientEmails.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_emailSettings.SmtpHost) ||
            string.IsNullOrWhiteSpace(_emailSettings.UserName) ||
            string.IsNullOrWhiteSpace(_emailSettings.Password) ||
            string.IsNullOrWhiteSpace(_emailSettings.FromEmail))
        {
            throw new InvalidOperationException("SMTP email settings are not configured.");
        }

        using var client = new SmtpClient();
        var secureSocketOptions = _emailSettings.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.Auto;

        await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.Port, secureSocketOptions, cancellationToken);
        await client.AuthenticateAsync(_emailSettings.UserName, _emailSettings.Password, cancellationToken);

        foreach (var recipientEmail in recipientEmails)
        {
            var message = BuildMessage(recipientEmail, hostName, meetingTitle, meetingCode, meetingLink);
            await client.SendAsync(message, cancellationToken);
        }

        await client.DisconnectAsync(true, cancellationToken);
    }

    private MimeMessage BuildMessage(
        string recipientEmail,
        string hostName,
        string meetingTitle,
        string meetingCode,
        string meetingLink)
    {
        var encodedHostName = WebUtility.HtmlEncode(hostName);
        var encodedMeetingTitle = WebUtility.HtmlEncode(meetingTitle);
        var encodedMeetingCode = WebUtility.HtmlEncode(meetingCode);
        var encodedMeetingLink = WebUtility.HtmlEncode(meetingLink);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
        message.To.Add(MailboxAddress.Parse(recipientEmail));
        message.Subject = $"{hostName} is inviting you to a video meeting";

        var builder = new BodyBuilder
        {
            HtmlBody = $"""
                <div style="margin:0;padding:32px 0;background:#f6f8fc;font-family:Arial,sans-serif;color:#202124;">
                  <div style="max-width:640px;margin:0 auto;background:#ffffff;border-radius:20px;overflow:hidden;border:1px solid #e4e7eb;">
                    <div style="padding:32px 36px;background:#eef4ff;">
                      <div style="font-size:28px;font-weight:700;color:#1a73e8;">Video Meet</div>
                    </div>
                    <div style="padding:36px;">
                      <p style="margin:0 0 18px;font-size:24px;line-height:1.35;font-weight:600;">{encodedHostName} is inviting you to a video meeting.</p>
                      <p style="margin:0 0 16px;font-size:16px;line-height:1.6;color:#5f6368;">Join <strong>{encodedMeetingTitle}</strong> using the button below.</p>
                      <div style="margin:30px 0;">
                        <a href="{encodedMeetingLink}" style="display:inline-block;padding:14px 24px;border-radius:999px;background:#1a73e8;color:#ffffff;text-decoration:none;font-size:15px;font-weight:700;">JOIN MEETING</a>
                      </div>
                      <div style="padding:18px 20px;border-radius:16px;background:#f8faff;border:1px solid #dce7fb;">
                        <div style="font-size:13px;color:#5f6368;margin-bottom:6px;">Meeting Code</div>
                        <div style="font-size:18px;font-weight:700;color:#202124;letter-spacing:0.08em;">{encodedMeetingCode}</div>
                      </div>
                      <p style="margin:24px 0 8px;font-size:13px;color:#5f6368;">Meeting Link</p>
                      <p style="margin:0;word-break:break-all;font-size:14px;color:#1a73e8;">{encodedMeetingLink}</p>
                    </div>
                  </div>
                </div>
                """
        };

        message.Body = builder.ToMessageBody();
        return message;
    }
}
