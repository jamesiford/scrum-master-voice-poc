namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Startup migration boundary, completed before schedulers and requests can write records.</summary>
public interface ITeamRecordMigration
{
    /// <summary>Copies authoritative legacy records and publishes the item schema marker.</summary>
    Task<int> RunAsync(CancellationToken ct);
}
