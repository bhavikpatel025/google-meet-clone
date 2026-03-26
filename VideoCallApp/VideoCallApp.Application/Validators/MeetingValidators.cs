using FluentValidation;
using VideoCallApp.Application.DTOs.Meeting;

namespace VideoCallApp.Application.Validators;

public class CreateMeetingRequestValidator : AbstractValidator<CreateMeetingRequestDto>
{
    public CreateMeetingRequestValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");
    }
}

public class JoinMeetingRequestValidator : AbstractValidator<JoinMeetingRequestDto>
{
    public JoinMeetingRequestValidator()
    {
        RuleFor(x => x.MeetingCode)
            .NotEmpty().WithMessage("Meeting code is required")
            .Length(8).WithMessage("Meeting code must be 8 characters");
    }
}

public class InviteParticipantsRequestValidator : AbstractValidator<InviteParticipantsRequestDto>
{
    public InviteParticipantsRequestValidator()
    {
        RuleFor(x => x.MeetingId)
            .GreaterThan(0).WithMessage("Meeting ID is required");

        RuleFor(x => x.HostName)
            .NotEmpty().WithMessage("Host name is required")
            .MaximumLength(200).WithMessage("Host name cannot exceed 200 characters");

        RuleFor(x => x.Emails)
            .NotEmpty().WithMessage("At least one email address is required");

        RuleForEach(x => x.Emails)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}
