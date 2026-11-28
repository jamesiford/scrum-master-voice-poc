namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Indexed record access while retaining collection reads for legacy reset compatibility.</summary>
internal interface IRecordCollectionStore : ICollectionStore
{
    /// <summary>Queries only matching records rather than loading historical standups.</summary>
    Task<IReadOnlyList<T>> QueryAsync<T>(string name, string? teamId, Guid? sessionId, string? status, string? key, CancellationToken ct);

    /// <summary>Writes one record, optionally preserving the first successful delivery.</summary>
    Task SaveRecordAsync<T>(string name, T value, bool createOnly, CancellationToken ct);
}
