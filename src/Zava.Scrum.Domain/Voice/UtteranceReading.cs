namespace Zava.Scrum.Domain.Voice;

/// <summary>
/// What the decision model read from one utterance, already parsed into typed values but not yet trusted:
/// <see cref="UtteranceAnalyser"/> grounds it against the roster and the sprint before anything acts on it.
/// </summary>
/// <param name="Intent">Step 1: what the speaker is doing (asking, updating, stepping away, ...); <see langword="null"/> when unreadable.</param>
/// <param name="TargetMemberId">The member it concerns (who is away, whose update is given), if any.</param>
/// <param name="ByMemberId">For a proxy, the member who speaks for the target.</param>
/// <param name="WorkItemIds">Step 2: the items the utterance is about.</param>
/// <param name="Comments">Step 3: what was said about them.</param>
/// <param name="Confidence">0 to 1; a reading under <see cref="UtteranceAnalyser.MinConfidence"/> is ignored.</param>
public sealed record UtteranceReading(
    UtteranceIntent? Intent,
    string? TargetMemberId,
    string? ByMemberId,
    IReadOnlyList<int> WorkItemIds,
    IReadOnlyList<ItemComment> Comments,
    double Confidence);
