using FluentValidation;

namespace Zava.Scrum.Application.Features.Teams.CreateTeam;

/// <summary>Validates <see cref="CreateTeamCommand"/>.</summary>
internal sealed class CreateTeamValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamValidator()
    {
        RuleFor(c => c.TeamId).NotEmpty();
        RuleFor(c => c.Input).NotNull();
        RuleFor(c => c.Input.Name).NotEmpty().MaximumLength(80).When(c => c.Input is not null);
        RuleFor(c => c.Input.StandupMinutes).InclusiveBetween(5, 60).When(c => c.Input is not null);
        RuleFor(c => c.Input.TimeZone).NotEmpty().When(c => c.Input is not null);
        RuleFor(c => c.Input.Ado).NotNull().When(c => c.Input is not null);
    }
}
