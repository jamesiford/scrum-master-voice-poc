using Zava.Scrum.Domain.Members;

namespace Zava.Scrum.Domain.Voice;

/// <summary>A participant's presentation name; it never grants query or approval authority.</summary>
/// <param name="ParticipantId">Bridge-local participant identifier.</param>
/// <param name="MemberId">Uniquely matched roster member, or null.</param>
/// <param name="DisplayName">Roster name for a matched Entra id, otherwise the guest's supplied name.</param>
/// <param name="Guest">Whether no unique Entra roster identity was matched.</param>
public sealed record ParticipantLabel(string ParticipantId, string? MemberId, string DisplayName, bool Guest)
{
    /// <summary>Resolves a unique Entra roster identity; display-name matches and duplicate identities remain guests.</summary>
    /// <param name="participantId">The bridge participant.</param>
    /// <param name="objectId">Non-anonymous Entra object id, when supplied by the calling adapter.</param>
    /// <param name="displayName">Fallback guest name, not an identity lookup.</param>
    /// <param name="roster">This session's owning team roster.</param>
    /// <returns>An honest presentation label without authorisation effects.</returns>
    public static ParticipantLabel Resolve(string participantId, Guid? objectId, string? displayName, IReadOnlyList<Member> roster)
    {
        ArgumentNullException.ThrowIfNull(roster);
        var matches = objectId is null || objectId == Guid.Empty ? [] : roster.Where(m => m.EntraObjectId == objectId).Take(2).ToArray();
        return matches.Length == 1
            ? new ParticipantLabel(participantId, matches[0].Id, matches[0].DisplayName, false)
            : new ParticipantLabel(participantId, null, string.IsNullOrWhiteSpace(displayName) ? "Guest" : displayName.Trim(), true);
    }
}
