using System.Text.RegularExpressions;

namespace Mcm.Scrum.Domain.Standups;

/// <summary>
/// Resolves "Maya can help" to a roster member in code (M9-R2). The model never picks the member, so an invented or ambiguous
/// name cannot create a follow-up for the wrong person (rule 2 in spirit: only roster ids reach a card).
/// </summary>
public static partial class UnblockerResolver
{
    private static readonly string[] Nobody = ["nobody", "no one", "noone", "nothing", "not sure", "don't know", "dont know", "i'm fine", "no need"];

    /// <summary>Finds the one roster member named in an answer.</summary>
    /// <param name="answer">What the blocked member said.</param>
    /// <param name="roster">The members of the standup.</param>
    /// <param name="blockedMemberId">The blocked member, who cannot be their own unblocker.</param>
    /// <returns>The member id, or <see langword="null"/> when nobody, or more than one person, was named.</returns>
    public static string? Resolve(string? answer, IReadOnlyList<MemberBrief> roster, string blockedMemberId)
    {
        ArgumentNullException.ThrowIfNull(roster);
        if (string.IsNullOrWhiteSpace(answer) || Nobody.Any(n => answer.Contains(n, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var words = Words().Matches(answer).Select(m => m.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var named = roster
            .Where(m => m.MemberId != blockedMemberId && Names(m).Any(n => Matches(n, words)))
            .Select(m => m.MemberId)
            .Distinct()
            .ToList();
        return named.Count == 1 ? named[0] : null;
    }

    private static IEnumerable<string> Names(MemberBrief member)
    {
        yield return member.DisplayName;
        var space = member.DisplayName.IndexOf(' ', StringComparison.Ordinal);
        if (space > 0)
        {
            yield return member.DisplayName[..space];
        }
    }

    private static bool Matches(string name, HashSet<string> words) =>
        name.Split(' ', StringSplitOptions.RemoveEmptyEntries).All(words.Contains);

    [GeneratedRegex(@"[\p{L}']+")]
    private static partial Regex Words();
}
