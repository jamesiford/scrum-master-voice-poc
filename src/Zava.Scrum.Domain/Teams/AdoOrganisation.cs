using System.Text.RegularExpressions;

namespace Zava.Scrum.Domain.Teams;

/// <summary>Rules for an Azure DevOps organisation URL. Only the two Microsoft-hosted forms are accepted, so a team cannot point the API at an arbitrary host.</summary>
public static partial class AdoOrganisation
{
    /// <summary>Normalises an organisation URL: no trailing slash, lower-case host.</summary>
    /// <param name="url">The URL as entered.</param>
    /// <returns>The normalised URL, or <see langword="null"/> when it is not <c>https://dev.azure.com/{name}</c> or <c>https://{name}.visualstudio.com</c>.</returns>
    public static string? Normalise(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        var host = uri.Host.ToLowerInvariant();
        if (host == "dev.azure.com")
        {
            var name = uri.AbsolutePath.Trim('/').Split('/')[0];
            return NameRegex().IsMatch(name) ? $"https://dev.azure.com/{name}" : null;
        }

        return host.EndsWith(".visualstudio.com", StringComparison.Ordinal) && NameRegex().IsMatch(host[..^".visualstudio.com".Length])
            ? $"https://{host}"
            : null;
    }

    /// <summary>Checks that a team id is a safe slug (it appears in routes, file names and cache keys).</summary>
    /// <param name="id">The id.</param>
    /// <returns><see langword="true"/> for 2 to 40 lower-case letters, digits and hyphens, starting with a letter.</returns>
    public static bool IsValidTeamId(string? id) => id is not null && TeamIdRegex().IsMatch(id);

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9-]{0,48}$", RegexOptions.CultureInvariant)]
    private static partial Regex NameRegex();

    [GeneratedRegex("^[a-z][a-z0-9-]{1,39}$", RegexOptions.CultureInvariant)]
    private static partial Regex TeamIdRegex();
}
