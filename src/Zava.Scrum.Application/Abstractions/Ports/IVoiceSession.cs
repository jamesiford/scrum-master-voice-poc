namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>One realtime voice conversation.</summary>
public interface IVoiceSession : IAsyncDisposable
{
    /// <summary>Reads events until the session closes.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The events.</returns>
    IAsyncEnumerable<VoiceEvent> ReadEventsAsync(CancellationToken ct);

    /// <summary>Sends incoming meeting audio (PCM16, 24 kHz, mono). The audio hot path bypasses the mediator.</summary>
    /// <param name="pcm">The audio.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    ValueTask SendAudioAsync(ReadOnlyMemory<byte> pcm, CancellationToken ct);

    /// <summary>Makes the agent speak, with turn-specific instructions.</summary>
    /// <param name="instructions">What to say; <see langword="null"/> continues after a tool result.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task CreateResponseAsync(string? instructions, CancellationToken ct);

    /// <summary>Cancels the agent's current response.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task CancelResponseAsync(CancellationToken ct);

    /// <summary>Adds text a participant said (for example a demo cast line) to the conversation without a response.</summary>
    /// <param name="text">The text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task AddUserTextAsync(string text, CancellationToken ct);

    /// <summary>Answers a tool call.</summary>
    /// <param name="callId">The call id.</param>
    /// <param name="output">The result as JSON.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task SubmitToolOutputAsync(string callId, string output, CancellationToken ct);
}
