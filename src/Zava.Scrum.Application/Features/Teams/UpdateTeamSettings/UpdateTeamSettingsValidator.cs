using FluentValidation;

namespace Mcm.Scrum.Application.Features.Teams.UpdateTeamSettings;

/// <summary>Validates <see cref="UpdateTeamSettingsCommand"/>.</summary>
internal sealed class UpdateTeamSettingsValidator : AbstractValidator<UpdateTeamSettingsCommand>
{
    public UpdateTeamSettingsValidator()
    {
        RuleFor(c => c.TeamId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.TimeZone).NotEmpty();
        RuleFor(c => c.Project).NotEmpty();
        RuleFor(c => c.AdoTeam).NotEmpty();
        RuleFor(c => c.StandupMinutes).InclusiveBetween(1, 60);
        RuleFor(c => c.StaleDays).InclusiveBetween(0, 30);
        RuleFor(c => c.EscalateBlockerAfterDays).InclusiveBetween(0, 30);
    }
}
