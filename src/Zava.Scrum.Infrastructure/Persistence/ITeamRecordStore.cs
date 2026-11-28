namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Per-item storage for the three growing collections, partitioned by team.</summary>
public interface ITeamRecordStore
{
    /// <summary>Reads one record when the team is known.</summary>
    Task<TeamRecord?> GetAsync(string teamId, string id, CancellationToken ct);

    /// <summary>Queries indexed fields; omitted filters preserve existing administrative list APIs.</summary>
    Task<IReadOnlyList<TeamRecord>> QueryAsync(string collection, string? teamId, string? sessionId, string? status, string? key, CancellationToken ct);

    /// <summary>Creates without replacing an existing item, for retries and additive migration.</summary>
    Task<bool> CreateAsync(TeamRecord record, CancellationToken ct);

    /// <summary>Replaces one record without rewriting unrelated records.</summary>
    Task UpsertAsync(TeamRecord record, CancellationToken ct);
}
