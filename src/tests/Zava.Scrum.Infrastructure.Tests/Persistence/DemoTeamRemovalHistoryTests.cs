using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Identity;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Infrastructure.Persistence;

namespace Zava.Scrum.Infrastructure.Tests.Persistence;

/// <summary>Production DI reads durable removals without external credentials or enabled reset configuration.</summary>
public sealed class DemoTeamRemovalHistoryTests
{
    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    public async Task Read_RestartWithResetDisabledAndInvalidAzureTenant_UsesOnlyCompletedRemovalJournal(bool completed, bool aborted, bool removed)
    {
        var path = Path.Combine(FindRoot(), ".m11-plan", "removal-history-test-" + Guid.NewGuid().ToString("N"));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Store:Mode"] = "Json",
            ["Store:DataPath"] = path,
            ["Azure:TenantId"] = "deliberately-invalid-locked-mode-tenant",
            ["Demo:ResetTenantId"] = Guid.Empty.ToString(),
        }).Build();
        try
        {
            await using (var first = Provider(configuration))
            {
                var tenant = Guid.NewGuid();
                var team = new Team("seed-team", "Seed", "UTC", 15, new AdoSettings("https://dev.azure.com/test", "Demo", "Seed", "Demo\\Seed"),
                    new TeamBehaviours(), "goal", IsDemo: true, DemoTenantId: tenant);
                var debris = team with { Id = "removed-team" };
                var followup = FollowUp.Create(Guid.NewGuid(), team.Id, Guid.NewGuid(), null, FollowUpKind.Followup, "restore", [],
                    new DateOnly(2026, 11, 27), DateTimeOffset.UnixEpoch);
                var state = first.GetRequiredService<IDemoStateStore>();
                await state.UpdateAsync(current =>
                {
                    current.Collections["teams.json"] = JsonSerializer.SerializeToElement(new Dictionary<string, Team> { [team.Id] = team }, JsonFile.SerializerOptions);
                    var id = Guid.NewGuid();
                    current.Operations[id] = new DemoReseedOperation(id, tenant, team, [team, debris], [], followup.ToSnapshot(), completed, aborted);
                    return true;
                }, default);
            }

            await using var restarted = Provider(configuration);
            var history = restarted.GetRequiredService<IDemoTeamRemovalHistory>();
            Assert.Equal(removed, await history.IsRemovedTeamAsync("removed-team", default));
            Assert.False(await history.IsRemovedTeamAsync("seed-team", default));
            Assert.False(await history.IsRemovedTeamAsync("unknown-team", default));
        }
        finally
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
    }

    private static ServiceProvider Provider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ZavaScrum.slnx")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
