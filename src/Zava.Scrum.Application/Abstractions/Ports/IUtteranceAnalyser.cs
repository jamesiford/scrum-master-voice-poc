using Zava.Scrum.Domain.Voice;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>
/// The decision model that reads one spoken utterance in three steps: what the speaker is doing, which work items it is about, and
/// what kind of comment it is (blocker, date change, commitment and so on). Read-only; it never decides anything by itself, because
/// <see cref="UtteranceAnalyser"/> checks every name and id it returns.
/// </summary>
public interface IUtteranceAnalyser
{
    /// <summary>Reads an utterance.</summary>
    /// <param name="context">The utterance and what the model may use to understand it.</param>
    /// <param name="ct">Cancellation token; the caller cancels it to enforce the latency budget.</param>
    /// <returns>The reading, or <see langword="null"/> when the model is off, unavailable, slow or unreadable (the rules then decide).</returns>
    Task<UtteranceReading?> ReadAsync(UtteranceContext context, CancellationToken ct);
}
