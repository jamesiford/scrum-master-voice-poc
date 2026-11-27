namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Atomically updates one durable publication document with concurrency control.</summary>
internal interface IDemoStateStore
{
    /// <summary>Reads one consistent document.</summary>
    Task<DemoState> ReadAsync(CancellationToken ct);

    /// <summary>Applies a deterministic local mutation and publishes it atomically; no network side effects belong in the callback.</summary>
    Task<T> UpdateAsync<T>(Func<DemoState, T> update, CancellationToken ct);
}
