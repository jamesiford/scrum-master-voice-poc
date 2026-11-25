using FluentValidation;

namespace Zava.Scrum.Application.Features.FollowUps.ApproveFollowUp;

/// <summary>Validates <see cref="ApproveFollowUpCommand"/>.</summary>
internal sealed class ApproveFollowUpValidator : AbstractValidator<ApproveFollowUpCommand>
{
    public ApproveFollowUpValidator()
    {
        RuleFor(c => c.FollowUpId).NotEmpty();
        RuleFor(c => c.Decision).NotNull().WithMessage("An identified approver is required.");
        RuleFor(c => c.Decision.Actor).NotNull().When(c => c.Decision is not null).WithMessage("An identified approver is required.");
    }
}
