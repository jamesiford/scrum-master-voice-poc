using Microsoft.Extensions.Options;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Infrastructure.Ado;

namespace Zava.Scrum.Api.Seeding;

/// <summary>
/// Registers the extra demo teams (<c>Demo:Teams</c>) in the team registry at start, as Active teams in the configured Azure DevOps organisation.
/// Idempotent: a team that already exists is left as it is. <c>Demo:PaymentsOnly</c> registers nothing, so the film shows one team.
/// </summary>
/// <remarks>DEMO: a deployment registers its teams through the registry API; this only saves the demo setup steps.</remarks>
public sealed class DemoTeamSeeder(IServiceScopeFactory scopes, IOptions<DemoOptions> demo, IOptions<AdoOptions> ado, ILogger<DemoTeamSeeder> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (demo.Value.PaymentsOnly || demo.Value.Teams.Count == 0)
        {
            return;
        }

        await using var scope = scopes.CreateAsyncScope();
        var teams = scope.ServiceProvider.GetRequiredService<ITeamRepository>();
        var history = scope.ServiceProvider.GetRequiredService<IDemoTeamRemovalHistory>();
        foreach (var d in demo.Value.Teams)
        {
            if (await teams.GetAsync(d.Id, cancellationToken) is not null
                || await history.IsRemovedTeamAsync(d.Id, cancellationToken))
            {
                continue;
            }

            await teams.SaveAsync(
                new Team(d.Id, d.Name, d.TimeZone, 15, new AdoSettings(ado.Value.Org, d.Project, d.AdoTeam, d.AreaPath), new TeamBehaviours(), d.SprintGoal, IsDemo: d.IsDemo, DemoTenantId: d.DemoTenantId),
                cancellationToken);
            logger.LogInformation("Registered demo team {TeamId} ({Project})", d.Id, d.Project);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
