namespace Zava.Scrum.MediaBot.Board;

/// <summary>A named theme with its upstream-calculated count.</summary>
public sealed record RetroTheme
{
    /// <summary>The theme name.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>The number of related notes.</summary>
    public int Count { get; init; }
}
