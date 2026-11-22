using FluentValidation;

namespace Mcm.Scrum.Application.Features.Standups.PrepareStandup;

/// <summary>Validates <see cref="PrepareStandupCommand"/>.</summary>
internal sealed class PrepareStandupValidator : AbstractValidator<PrepareStandupCommand>
{
    public PrepareStandupValidator() => RuleFor(c => c.TeamId).NotEmpty();
}
