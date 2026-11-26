using System.Text.Json;

namespace Zava.Scrum.MediaBot.Board;

/// <summary>A presentation-only snapshot of the gateway's shared board, not a source of work-item truth.</summary>
public abstract record BoardState
{
    /// <summary>Whether the ceremony has finished.</summary>
    public bool Ended { get; init; }

    /// <summary>The sprint's display name.</summary>
    public string Sprint { get; init; } = string.Empty;

    /// <summary>The current speaker's display name, or null for the agenda.</summary>
    public string? Speaker { get; init; }

    /// <summary>False for native ephemeral meetings; absent preserves the original browser completion wording.</summary>
    public bool? Recording { get; init; }

    /// <summary>Reads a board message using the same retro discriminator as the browser protocol.</summary>
    public static BoardState FromJson(JsonElement message)
    {
        return message.TryGetProperty("kind", out _)
            ? message.Deserialize<RetroBoardState>(JsonSerializerOptions.Web)!
            : message.Deserialize<StandupBoardState>(JsonSerializerOptions.Web)!;
    }
}
