using FluentValidation;

namespace Mcm.Scrum.Application.Features.FollowUps.ApproveFollowUp;

/// <summary>Validates <see cref="ApproveFollowUpCommand"/>.</summary>
internal sealed class ApproveFollowUpValidator : AbstractValidator<ApproveFollowUpCommand>
{
    public ApproveFollowUpValidator()
    {
        RuleFor(c => c.FollowUpId).NotEmpty();
        RuleFor(c => c.Approver).NotEmpty().MaximumLength(100);
    }
}
