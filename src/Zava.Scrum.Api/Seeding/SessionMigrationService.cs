using Microsoft.Extensions.Hosting;

namespace Zava.Scrum.Api.Seeding;

/// <summary>
/// Moves the legacy single <c>sessions.json</c> document into one item per session at start (ADR 0021), before the scheduler or any request
/// reads a session. Idempotent, so every instance may run it and a restart repeats it safely.
/// </summary>
public sealed class SessionMigrationService(IServiceScopeFactory scopes) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<Zava.Scrum.Infrastructure.Persistence.SessionMigration>().RunAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<Zava.Scrum.Infrastructure.Persistence.ITeamRecordMigration>().RunAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
