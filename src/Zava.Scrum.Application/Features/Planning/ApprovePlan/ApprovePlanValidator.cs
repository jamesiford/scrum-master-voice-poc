using FluentValidation;

namespace Zava.Scrum.Application.Features.Planning.ApprovePlan;

/// <summary>Validates <see cref="ApprovePlanCommand"/>.</summary>
internal sealed class ApprovePlanValidator : AbstractValidator<ApprovePlanCommand>
{
    public ApprovePlanValidator()
    {
        RuleFor(c => c.PackId).NotEmpty();
        RuleFor(c => c.Decision).NotNull();
        RuleFor(c => c.Decision.Actor).NotNull().When(c => c.Decision is not null);
        RuleFor(c => c.SelectedItemIds).NotNull();
    }
}
