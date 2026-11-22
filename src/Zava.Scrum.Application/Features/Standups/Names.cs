namespace Mcm.Scrum.Application.Features.Standups;

/// <summary>Name helpers for speech.</summary>
public static class Names
{
    /// <summary>Gets the first name from a display name such as "Priya S.".</summary>
    /// <param name="displayName">Display name.</param>
    /// <returns>The first word.</returns>
    public static string First(string displayName)
    {
        ArgumentNullException.ThrowIfNull(displayName);
        var space = displayName.IndexOf(' ', StringComparison.Ordinal);
        return space < 0 ? displayName : displayName[..space];
    }
}
