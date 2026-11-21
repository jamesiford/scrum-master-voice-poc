using FluentValidation;

namespace Mcm.Scrum.Application.Features.Schedules.UpdateSchedule;

/// <summary>Validates <see cref="UpdateScheduleCommand"/>.</summary>
internal sealed class UpdateScheduleValidator : AbstractValidator<UpdateScheduleCommand>
{
    public UpdateScheduleValidator()
    {
        RuleFor(c => c.TeamId).NotEmpty();
        RuleFor(c => c.StartTime).Must(t => TimeOnly.TryParseExact(t, "HH:mm", out _)).WithMessage("Start time must be HH:mm.");
        RuleFor(c => c.Days).Must(d => d.All(x => Enum.TryParse<DayOfWeek>(x, out _))).WithMessage("Days must be weekday names.");
        RuleFor(c => c.Mode).Must(m => m is "simulated" or "acs").WithMessage("Mode must be 'simulated' or 'acs'.");
        RuleFor(c => c.JoinUrl).NotEmpty().When(c => c.Mode == "acs" && c.Enabled).WithMessage("A Teams meeting link is required for acs mode.");
    }
}
