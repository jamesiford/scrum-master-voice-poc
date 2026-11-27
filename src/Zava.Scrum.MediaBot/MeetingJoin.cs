namespace Zava.Scrum.MediaBot;

/// <summary>The API's team-scoped join request; the API verifies it before any Graph call.</summary>
/// <param name="SessionId">The live session.</param>
/// <param name="TeamId">The session's team.</param>
/// <param name="JoinUrl">The stored calendar link.</param>
/// <param name="BridgeToken">The session-bound gateway capability.</param>
/// <param name="ApiBase">The configured, trusted API origin.</param>
/// <param name="TraceParent">The session trace propagated from the authenticated API.</param>
public sealed record MeetingJoin(Guid SessionId, string TeamId, string JoinUrl, string BridgeToken, string ApiBase, string? TraceParent = null);
