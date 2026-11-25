using FluentValidation;

namespace Zava.Scrum.Application.Features.FollowUps.RejectFollowUp;

/// <summary>Validates <see cref="RejectFollowUpCommand"/>.</summary>
internal sealed class RejectFollowUpValidator : AbstractValidator<RejectFollowUpCommand>
{
    public RejectFollowUpValidator()
    {
        RuleFor(c => c.FollowUpId).NotEmpty();
        RuleFor(c => c.Decision).NotNull().WithMessage("An identified approver is required.");
        RuleFor(c => c.Decision.Actor).NotNull().When(c => c.Decision is not null).WithMessage("An identified approver is required.");
    }
}
