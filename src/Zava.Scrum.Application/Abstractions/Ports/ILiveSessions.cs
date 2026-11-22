namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>The standups currently running on this API instance. Lets the portal watch and steer a live standup.</summary>
public interface ILiveSessions
{
    /// <summary>Sends a control action (mute, unmute, skip, takeover, release, end) to a running standup.</summary>
    /// <param name="sessionId">The standup.</param>
    /// <param name="action">The action.</param>
    /// <returns><see langword="true"/> when the standup is running here.</returns>
    bool TrySend(Guid sessionId, string action);

    /// <summary>Gets the latest floor and timebox state pushed by the voice gateway.</summary>
    /// <param name="sessionId">The standup.</param>
    /// <returns>The state as JSON, or <see langword="null"/> when the standup is not running here.</returns>
    string? GetState(Guid sessionId);
}
