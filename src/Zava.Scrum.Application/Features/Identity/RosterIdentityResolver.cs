using Microsoft.Extensions.Logging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Members;

namespace Zava.Scrum.Application.Features.Identity;

/// <summary>
/// Gives each roster member an Entra object id: resolved once from the mapped user principal name (Azure DevOps identity mapping, then
/// Microsoft Graph), stored, and re-checked when a session is prepared. A member with no resolvable account (a demo cast member)
/// keeps none and can never approve in Teams. Display names are never used to find an identity.
/// </summary>
internal sealed class RosterIdentityResolver(IIdentityDirectory directory, ILogger<RosterIdentityResolver> logger)
{
    /// <summary>Resolves missing ids and verifies stored ones.</summary>
    /// <param name="roster">The roster.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The roster with object ids filled in; unchanged members are returned as they were.</returns>
    public async Task<IReadOnlyList<Member>> ResolveAsync(IReadOnlyList<Member> roster, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(roster);
        var result = new List<Member>(roster.Count);
        foreach (var member in roster)
        {
            result.Add(await ResolveOneAsync(member, ct));
        }

        return result;
    }

    private async Task<Member> ResolveOneAsync(Member member, CancellationToken ct)
    {
        if (member.AdoUniqueName?.Contains('@', StringComparison.Ordinal) != true)
        {
            return member;
        }

        var found = await directory.FindObjectIdAsync(member.AdoUniqueName, ct);
        if (found is null || found == member.EntraObjectId)
        {
            return member;
        }

        if (member.EntraObjectId is not null)
        {
            logger.LogWarning("Roster member {MemberId} now resolves to a different Entra object id; the stored one is replaced", member.Id);
        }

        return member with { EntraObjectId = found };
    }
}
