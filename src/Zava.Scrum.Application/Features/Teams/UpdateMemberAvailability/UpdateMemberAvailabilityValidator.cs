using FluentValidation;

namespace Zava.Scrum.Application.Features.Teams.UpdateMemberAvailability;

/// <summary>Validates <see cref="UpdateMemberAvailabilityCommand"/>.</summary>
internal sealed class UpdateMemberAvailabilityValidator : AbstractValidator<UpdateMemberAvailabilityCommand>
{
    public UpdateMemberAvailabilityValidator()
    {
        RuleFor(c => c.TeamId).NotEmpty();
        RuleFor(c => c.MemberId).NotEmpty();
        RuleFor(c => c.Percent).InclusiveBetween(0, 100);
    }
}
