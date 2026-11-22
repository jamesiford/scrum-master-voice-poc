using FluentValidation;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Features.Ceremonies.CreateCeremonyPack;

/// <summary>Validates <see cref="CreateCeremonyPackCommand"/>.</summary>
internal sealed class CreateCeremonyPackValidator : AbstractValidator<CreateCeremonyPackCommand>
{
    public CreateCeremonyPackValidator()
    {
        RuleFor(c => c.TeamId).NotEmpty();
        RuleFor(c => c.Kind).IsInEnum().NotEqual(CeremonyKind.DailyScrum).WithMessage("The daily scrum has no ceremony pack.");
        RuleFor(c => c.Kind).NotEqual(CeremonyKind.SprintPlanning).WithMessage("Sprint planning has its own pack: use PreparePlanningCommand (POST /api/planning/prepare).");
    }
}
