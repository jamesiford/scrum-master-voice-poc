namespace Zava.Scrum.Domain.Standups;

/// <summary>
/// What happened when the agent host was warmed for a standup (ADR 0021): which route carries the voice, whether a sandbox was started, and how long it
/// took, so the cold start is visible on the session and not paid by the first turn.
/// </summary>
/// <param name="Route">The voice route, <c>direct</c> or <c>hosted</c>.</param>
/// <param name="Warmed">Whether a host was started and answered. <see langword="false"/> for the direct route (nothing to warm) or a failed warm-up.</param>
/// <param name="Milliseconds">How long the warm-up took.</param>
/// <param name="At">When it ran.</param>
/// <param name="Note">Why it was not warmed, if it was not.</param>
public sealed record HostWarmUp(string Route, bool Warmed, int Milliseconds, DateTimeOffset At, string? Note = null);
