namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>
/// Remembers the Teams conversations the bot may message proactively. The reference is opaque to the Application layer.
/// </summary>
public interface IConversationStore
{
    /// <summary>Stores or replaces a conversation reference.</summary>
    /// <param name="key">Stable key, for example the Teams conversation id.</param>
    /// <param name="referenceJson">Serialized reference.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task SaveAsync(string key, string referenceJson, CancellationToken ct);

    /// <summary>Lists stored references.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Key and serialized reference pairs.</returns>
    Task<IReadOnlyList<KeyValuePair<string, string>>> ListAsync(CancellationToken ct);
}
