namespace Zava.Scrum.MediaBot.Board;

/// <summary>An action proposal, not approval or permission to write to Azure DevOps.</summary>
public sealed record RetroAction
{
    /// <summary>The proposed action text.</summary>
    public string Text { get; init; } = string.Empty;
    /// <summary>The proposed owner's name.</summary>
    public string Owner { get; init; } = string.Empty;
}
