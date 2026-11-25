using System.Security.Cryptography;
using System.Text;

namespace Zava.Scrum.Domain.FollowUps;

/// <summary>
/// The SHA-256 of the canonical form of the proposed changes. An approval record stores it, so what was approved can later be
/// proved equal to what was proposed and written (ADR 0017).
/// </summary>
public static class ProposalHash
{
    /// <summary>Computes the hash.</summary>
    /// <param name="changes">The proposed changes, in order.</param>
    /// <returns>Upper-case hex SHA-256.</returns>
    public static string Of(IReadOnlyList<ProposedChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var canonical = new StringBuilder();
        foreach (var c in changes)
        {
            canonical.Append(c.WorkItemId).Append('|').Append(c.Op).Append('|').Append(Clean(c.Value)).Append('|').Append(Clean(c.OwnerKey));
            if (c.New is { } n)
            {
                canonical.Append('|').Append(Clean(n.Type)).Append('|').Append(Clean(n.Title)).Append('|').Append(Clean(n.Description))
                    .Append('|').Append(string.Join(',', n.Tags.OrderBy(t => t, StringComparer.Ordinal))).Append('|').Append(Clean(n.IterationPath)).Append('|').Append(Clean(n.AssignedTo));
            }

            canonical.Append('\n');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }

    private static string Clean(string? value) => (value ?? string.Empty).Replace('\n', ' ').Replace('\r', ' ').Replace('|', '/').Trim();
}
