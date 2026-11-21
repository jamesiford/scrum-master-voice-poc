using FluentValidation;

namespace Mcm.Scrum.Application.Features.Standups.StartStandup;

/// <summary>Validates <see cref="StartStandupCommand"/>.</summary>
internal sealed class StartStandupValidator : AbstractValidator<StartStandupCommand>
{
    public StartStandupValidator()
    {
        RuleFor(c => c.SessionId).NotEmpty();
        RuleFor(c => c.Mode).Must(m => m is "simulated" or "acs").WithMessage("Mode must be 'simulated' or 'acs'.");
        RuleFor(c => c.JoinUrl).NotEmpty().When(c => c.Mode == "acs").WithMessage("A join URL is required for acs mode.");
        RuleFor(c => c.StandupMinutes).InclusiveBetween(1, 60).When(c => c.StandupMinutes.HasValue);
    }
}
