using FluentValidation;

namespace Mcm.Scrum.Application.Features.Planning.PreparePlanning;

/// <summary>Validates <see cref="PreparePlanningCommand"/>.</summary>
internal sealed class PreparePlanningValidator : AbstractValidator<PreparePlanningCommand>
{
    public PreparePlanningValidator() => RuleFor(c => c.TeamId).NotEmpty();
}
