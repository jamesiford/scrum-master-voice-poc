using FluentValidation;

namespace Zava.Scrum.Application.Features.Planning.RejectPlan;

/// <summary>Validates <see cref="RejectPlanCommand"/>.</summary>
internal sealed class RejectPlanValidator : AbstractValidator<RejectPlanCommand>
{
    public RejectPlanValidator()
    {
        RuleFor(c => c.PackId).NotEmpty();
        RuleFor(c => c.Rejecter).NotEmpty().MaximumLength(100);
    }
}
