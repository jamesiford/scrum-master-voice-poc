namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>
/// Starts the agent host sandbox for a team's standup before it begins, so the first voice turn does not pay the cold start (ADR 0021). The hosted
/// route calls the agent once with the session id the standup will use; the direct route has no host and reports that.
/// </summary>
public interface IAgentHostWarmer
{
    /// <summary>Warms the host for one standup.</summary>
    /// <param name="teamId">The team, resolved by the caller; the host never infers it.</param>
    /// <param name="sessionId">The standup session, which is also the hosted agent session id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The route, whether a host answered, and how long it took.</returns>
    Task<HostWarmResult> WarmAsync(string teamId, Guid sessionId, CancellationToken ct);
}

/// <summary>The outcome of one warm-up.</summary>
/// <param name="Route"><c>direct</c> or <c>hosted</c>.</param>
/// <param name="Warmed">Whether a host answered.</param>
/// <param name="Milliseconds">How long it took.</param>
/// <param name="Note">Why it was not warmed, if it was not.</param>
public sealed record HostWarmResult(string Route, bool Warmed, int Milliseconds, string? Note = null);
