namespace Zava.Scrum.MediaBot.Board;

/// <summary>The retrospective columns, themes and agreed actions, mirroring protocol.ts.</summary>
public sealed record RetroBoardState : BoardState
{
    /// <summary>The browser protocol discriminator.</summary>
    public string Kind { get; init; } = "retro";
    /// <summary>The protocol phase, Round or Actions.</summary>
    public string Phase { get; init; } = "Round";
    /// <summary>Columns in gateway order.</summary>
    public IReadOnlyList<RetroColumn> Columns { get; init; } = [];
    /// <summary>The themes calculated upstream.</summary>
    public IReadOnlyList<RetroTheme> Themes { get; init; } = [];
    /// <summary>The proposed actions; only the first three are painted.</summary>
    public IReadOnlyList<RetroAction> Actions { get; init; } = [];
    /// <summary>The roster, retained for protocol compatibility; the retro canvas does not paint it.</summary>
    public IReadOnlyList<BoardMember> Team { get; init; } = [];
}
