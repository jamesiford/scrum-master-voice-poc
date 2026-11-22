using FluentValidation;

namespace Zava.Scrum.Application.Features.Planning.ApprovePlan;

/// <summary>Validates <see cref="ApprovePlanCommand"/>.</summary>
internal sealed class ApprovePlanValidator : AbstractValidator<ApprovePlanCommand>
{
    public ApprovePlanValidator()
    {
        RuleFor(c => c.PackId).NotEmpty();
        RuleFor(c => c.Approver).NotEmpty().MaximumLength(100);
        RuleFor(c => c.SelectedItemIds).NotNull();
    }
}
