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
