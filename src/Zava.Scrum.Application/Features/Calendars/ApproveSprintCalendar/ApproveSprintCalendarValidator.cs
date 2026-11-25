using FluentValidation;

namespace Zava.Scrum.Application.Features.Calendars.ApproveSprintCalendar;

/// <summary>Validates <see cref="ApproveSprintCalendarCommand"/>.</summary>
internal sealed class ApproveSprintCalendarValidator : AbstractValidator<ApproveSprintCalendarCommand>
{
    public ApproveSprintCalendarValidator()
    {
        RuleFor(c => c.CalendarId).NotEmpty();
        RuleFor(c => c.Decision).NotNull();
        RuleFor(c => c.Decision.Actor).NotNull().When(c => c.Decision is not null);
    }
}
