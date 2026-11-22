using FluentValidation;

namespace Zava.Scrum.Application.Features.Standups.ControlStandup;

/// <summary>Validates <see cref="ControlStandupCommand"/>.</summary>
internal sealed class ControlStandupValidator : AbstractValidator<ControlStandupCommand>
{
    public ControlStandupValidator()
    {
        RuleFor(c => c.Action).Must(a => a is "mute" or "unmute" or "skip" or "takeover" or "release" or "end" or "begin")
            .WithMessage("Action must be begin, mute, unmute, skip, takeover, release or end.");
    }
}
