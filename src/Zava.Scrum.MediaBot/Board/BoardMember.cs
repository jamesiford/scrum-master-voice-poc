namespace Zava.Scrum.MediaBot.Board;

/// <summary>A roster member's already-calculated coverage.</summary>
public sealed record BoardMember
{
    /// <summary>The display name.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>The total assigned item count.</summary>
    public int Total { get; init; }
    /// <summary>The covered item count.</summary>
    public int Covered { get; init; }
    /// <summary>An optional availability label.</summary>
    public string? Status { get; init; }
}
