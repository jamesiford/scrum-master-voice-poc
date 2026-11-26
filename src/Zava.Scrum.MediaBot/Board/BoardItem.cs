namespace Zava.Scrum.MediaBot.Board;

/// <summary>A work item already scoped and validated by the gateway.</summary>
public sealed record BoardItem
{
    /// <summary>The real work-item id supplied by the gateway.</summary>
    public int Id { get; init; }
    /// <summary>The owner's display name.</summary>
    public string? Owner { get; init; }
    /// <summary>The work-item title.</summary>
    public string Title { get; init; } = string.Empty;
    /// <summary>The work-item type.</summary>
    public string Type { get; init; } = string.Empty;
    /// <summary>The work-item state.</summary>
    public string State { get; init; } = string.Empty;
    /// <summary>Flags calculated upstream.</summary>
    public IReadOnlyList<string> Flags { get; init; } = [];
    /// <summary>Whether this item has been covered.</summary>
    public bool Covered { get; init; }
}
