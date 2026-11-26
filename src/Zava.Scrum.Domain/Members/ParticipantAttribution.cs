namespace Zava.Scrum.Domain.Members;

/// <summary>Matches meeting participants only by verified object id in the session's team (ADR 0023).</summary>
public static class ParticipantAttribution
{
    /// <summary>Resolves a participant; anonymous, cross-team or ambiguous identities remain guests.</summary>
    /// <param name="teamId">The session's team.</param>
    /// <param name="objectId">The participant's Entra identity, never a display name.</param>
    /// <param name="roster">The roster read for this session.</param>
    /// <returns>The unique member, or <see langword="null"/> for a guest.</returns>
    public static Member? Resolve(string teamId, Guid? objectId, IReadOnlyList<Member> roster)
    {
        ArgumentNullException.ThrowIfNull(roster);
        if (objectId is null || objectId == Guid.Empty)
        {
            return null;
        }

        var matches = roster.Where(m => m.TeamId == teamId && m.EntraObjectId == objectId).Take(2).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }
}
