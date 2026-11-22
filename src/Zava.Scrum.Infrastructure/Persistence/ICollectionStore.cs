namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>
/// Stores one JSON document per named collection. The JSON-file and Cosmos repositories share it, so every repository behaves
/// the same on both stores (SOLID: L). Whole-collection documents are fine for a PoC.
/// </summary>
/// <remarks>PROD: one Cosmos item per record, partitioned by team, with ETag concurrency and no whole-collection rewrites.</remarks>
public interface ICollectionStore
{
    /// <summary>Reads a collection.</summary>
    /// <typeparam name="T">The collection type.</typeparam>
    /// <param name="name">Collection name, for example <c>sessions.json</c>.</param>
    /// <param name="empty">Creates the value when nothing is stored.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stored value, or <paramref name="empty"/>.</returns>
    Task<T> ReadAsync<T>(string name, Func<T> empty, CancellationToken ct);

    /// <summary>Replaces a collection.</summary>
    /// <typeparam name="T">The collection type.</typeparam>
    /// <param name="name">Collection name.</param>
    /// <param name="value">The new value.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task WriteAsync<T>(string name, T value, CancellationToken ct);

    /// <summary>Deletes a collection. Safe when it does not exist.</summary>
    /// <param name="name">Collection name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task DeleteAsync(string name, CancellationToken ct);
}
