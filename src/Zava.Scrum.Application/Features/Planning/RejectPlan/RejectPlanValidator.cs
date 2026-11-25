using FluentValidation;

namespace Zava.Scrum.Application.Features.Planning.RejectPlan;

/// <summary>Validates <see cref="RejectPlanCommand"/>.</summary>
internal sealed class RejectPlanValidator : AbstractValidator<RejectPlanCommand>
{
    public RejectPlanValidator()
    {
        RuleFor(c => c.PackId).NotEmpty();
        RuleFor(c => c.Decision).NotNull();
        RuleFor(c => c.Decision.Actor).NotNull().When(c => c.Decision is not null);
    }
}
