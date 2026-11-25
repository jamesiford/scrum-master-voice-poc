using Microsoft.Extensions.Logging;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>
/// Moves the legacy single <c>sessions.json</c> document into one item per session (ADR 0021). Idempotent: items are upserted by id, and the
/// legacy document is deleted only after every session is in the new store, so a crash part way is repeated safely on the next start.
/// </summary>
public sealed class SessionMigration(ICollectionStore legacy, ISessionItemStore items, ILogger<SessionMigration> logger)
{
    private const string LegacyCollection = "sessions.json";

    /// <summary>Runs the migration.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>How many sessions were moved.</returns>
    public async Task<int> RunAsync(CancellationToken ct)
    {
        var old = await legacy.ReadAsync<List<StandupSessionSnapshot>>(LegacyCollection, () => [], ct);
        if (old.Count == 0)
        {
            await legacy.DeleteAsync(LegacyCollection, ct);
            return 0;
        }

        foreach (var snapshot in old)
        {
            await items.UpsertAsync(snapshot, ct);
        }

        await legacy.DeleteAsync(LegacyCollection, ct);
        logger.LogInformation("Moved {Count} sessions from {Legacy} to one item per session, partitioned by team", old.Count, LegacyCollection);
        return old.Count;
    }
}
