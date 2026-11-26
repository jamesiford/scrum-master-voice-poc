namespace Zava.Scrum.MediaBot.Board;

/// <summary>A retrospective column; only its last five notes are painted.</summary>
public sealed record RetroColumn
{
    /// <summary>The gateway's column key.</summary>
    public string Key { get; init; } = string.Empty;
    /// <summary>The heading.</summary>
    public string Label { get; init; } = string.Empty;
    /// <summary>Notes in gateway order.</summary>
    public IReadOnlyList<RetroNote> Notes { get; init; } = [];
}
