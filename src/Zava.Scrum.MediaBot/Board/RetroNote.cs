namespace Zava.Scrum.MediaBot.Board;

/// <summary>A retrospective note supplied by the gateway.</summary>
public sealed record RetroNote
{
    /// <summary>The note text.</summary>
    public string Text { get; init; } = string.Empty;
    /// <summary>The contributing member's name.</summary>
    public string Member { get; init; } = string.Empty;
}
