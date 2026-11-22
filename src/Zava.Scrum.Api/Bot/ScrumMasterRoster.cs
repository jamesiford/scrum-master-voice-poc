namespace Mcm.Scrum.Api.Bot;

/// <summary>
/// Decides who counts as the Scrum Master for chat commands. The names come from <c>Team:ScrumMasters</c> (a list or a comma-separated string).
/// A presentation-layer check only: approving a plan still needs a named person, like every follow-up (F7).
/// </summary>
/// <remarks>PROD: an Entra group or app role instead of display names.</remarks>
public sealed class ScrumMasterRoster(IConfiguration configuration)
{
    /// <summary>Checks whether a Teams display name is a configured Scrum Master.</summary>
    /// <param name="displayName">The sender's display name from the activity.</param>
    /// <returns><see langword="true"/> when the name is configured. Nobody is a Scrum Master when none are configured.</returns>
    public bool IsScrumMaster(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return false;
        }

        var section = configuration.GetSection("Team:ScrumMasters");
        var names = section.Get<string[]>() ?? [];
        if (names.Length == 0 && !string.IsNullOrWhiteSpace(section.Value))
        {
            names = section.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return names.Any(n => string.Equals(n.Trim(), displayName.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
