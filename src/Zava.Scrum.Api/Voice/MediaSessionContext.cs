using Zava.Scrum.Application.Features.Standups;
using Zava.Scrum.Domain.Members;

namespace Zava.Scrum.Api.Voice;

/// <summary>Prepared facts for the non-recording media gateway; no writable port crosses this boundary.</summary>
/// <param name="Session">The authorised live session.</param>
/// <param name="Roster">Its team's verified roster.</param>
/// <param name="Instructions">The non-recording facilitation prompt.</param>
/// <param name="JoinUrl">The session's stored calendar link.</param>
/// <param name="BoardMode">The configured real board adapter.</param>
public sealed record MediaSessionContext(SessionDto Session, IReadOnlyList<Member> Roster, string Instructions, string JoinUrl, string BoardMode = "Share");
