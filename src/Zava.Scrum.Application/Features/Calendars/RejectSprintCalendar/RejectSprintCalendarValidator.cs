using FluentValidation;

namespace Zava.Scrum.Application.Features.Calendars.RejectSprintCalendar;

/// <summary>Validates <see cref="RejectSprintCalendarCommand"/>.</summary>
internal sealed class RejectSprintCalendarValidator : AbstractValidator<RejectSprintCalendarCommand>
{
    public RejectSprintCalendarValidator()
    {
        RuleFor(c => c.CalendarId).NotEmpty();
        RuleFor(c => c.Decision).NotNull();
        RuleFor(c => c.Decision.Actor).NotNull().When(c => c.Decision is not null);
    }
}
