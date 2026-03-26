namespace VideoCallApp.Application.Interfaces;

public interface IMeetingInvitationEmailService
{
    Task SendMeetingInvitationsAsync(
        IReadOnlyCollection<string> recipientEmails,
        string hostName,
        string meetingTitle,
        string meetingCode,
        string meetingLink,
        CancellationToken cancellationToken = default);
}
