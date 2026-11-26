namespace Zava.Scrum.MediaBot.Board;

/// <summary>The standup agenda or current speaker's items, mirroring protocol.ts.</summary>
public sealed record StandupBoardState : BoardState
{
    /// <summary>The sprint goal, clipped by the renderer just as in the browser.</summary>
    public string Goal { get; init; } = string.Empty;

    /// <summary>The number of days left, calculated upstream.</summary>
    public int DaysLeft { get; init; }

    /// <summary>The highlighted work-item id, if any.</summary>
    public int? Focus { get; init; }

    /// <summary>Items in gateway order; only the first six are painted.</summary>
    public IReadOnlyList<BoardItem> Items { get; init; } = [];

    /// <summary>The roster and its calculated coverage.</summary>
    public IReadOnlyList<BoardMember> Team { get; init; } = [];
}
