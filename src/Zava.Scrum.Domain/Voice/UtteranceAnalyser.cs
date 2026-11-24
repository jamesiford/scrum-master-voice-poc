using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Voice;

/// <summary>
/// Turns one utterance into a grounded <see cref="UtteranceAnalysis"/>. The decision model proposes (<see cref="UtteranceReading"/>);
/// this class disposes: every member must be on the roster, every work item id must be in the sprint (rule 2), the intent must be
/// possible for the speaker's role, and anything doubtful falls back to <see cref="UtteranceClassifier"/> and <see cref="ItemMentions"/>.
/// A model never gets to invent a person or a ticket.
/// </summary>
public static class UtteranceAnalyser
{
    /// <summary>The lowest model confidence that is acted on; below it the rules decide.</summary>
    public const double MinConfidence = 0.6;

    /// <summary>Analyses an utterance.</summary>
    /// <param name="text">What was said.</param>
    /// <param name="speakerId">Who said it.</param>
    /// <param name="holderId">Whose turn it is, if anyone's.</param>
    /// <param name="roster">Everyone in the ceremony.</param>
    /// <param name="ownItems">The items of the member whose turn it is, used by the rules to match titles.</param>
    /// <param name="knownIds">Every sprint item id; anything else is dropped.</param>
    /// <param name="reading">What the model read, or <see langword="null"/> when it was off, failed or timed out.</param>
    /// <returns>The analysis; its <see cref="UtteranceAnalysis.Source"/> says who decided.</returns>
    public static UtteranceAnalysis Analyse(
        string? text,
        string speakerId,
        string? holderId,
        IReadOnlyList<NameRef> roster,
        IReadOnlyList<BriefItem> ownItems,
        IReadOnlySet<int> knownIds,
        UtteranceReading? reading)
    {
        ArgumentNullException.ThrowIfNull(roster);
        ArgumentNullException.ThrowIfNull(ownItems);
        ArgumentNullException.ThrowIfNull(knownIds);
        var byRules = new UtteranceAnalysis(
            UtteranceClassifier.Classify(text, speakerId, holderId, roster),
            ItemMentions.Detect(text, ownItems, knownIds),
            [],
            AnalysisSource.Rules);
        if (reading?.Intent is not { } intent || reading.Confidence < MinConfidence)
        {
            return byRules;
        }

        var interpretation = Ground(intent, reading, speakerId, holderId, roster);
        if (interpretation is null)
        {
            return byRules;
        }

        var ids = Known(reading.WorkItemIds, knownIds);
        return new UtteranceAnalysis(interpretation, ids.Count > 0 ? ids : byRules.WorkItemIds, GroundComments(reading.Comments, ids, knownIds), AnalysisSource.Model);
    }

    // Returns null when the model's answer cannot be made consistent with the roster, so the caller uses the rules.
    private static Interpretation? Ground(UtteranceIntent intent, UtteranceReading reading, string speakerId, string? holderId, IReadOnlyList<NameRef> roster)
    {
        var onRoster = roster.Select(r => r.MemberId).ToHashSet();
        var target = reading.TargetMemberId is { } t && onRoster.Contains(t) ? t : null;
        var by = reading.ByMemberId is { } b && onRoster.Contains(b) ? b : null;
        var isHolder = speakerId == holderId;
        switch (intent)
        {
            case UtteranceIntent.StepAway:
                return target is null ? null : new Interpretation(intent, target);
            case UtteranceIntent.ProxyUpdate:
                by ??= speakerId;
                return target is null || target == by ? null : new Interpretation(intent, target, by);
            case UtteranceIntent.BackNow:
                return isHolder ? null : new Interpretation(intent, speakerId);

            // The role decides between giving an update and chiming in, whatever the model says.
            case UtteranceIntent.Update or UtteranceIntent.OutOfTurnUpdate:
                return new Interpretation(isHolder ? UtteranceIntent.Update : UtteranceIntent.OutOfTurnUpdate, speakerId);
            case UtteranceIntent.Chatter:
                return new Interpretation(isHolder ? UtteranceIntent.Update : UtteranceIntent.Chatter, speakerId);
            default:
                return new Interpretation(intent, speakerId);
        }
    }

    private static List<int> Known(IReadOnlyList<int> ids, IReadOnlySet<int> knownIds) =>
        ids.Where(knownIds.Contains).Distinct().ToList();

    private static List<ItemComment> GroundComments(IReadOnlyList<ItemComment> comments, List<int> utteranceIds, IReadOnlySet<int> knownIds)
    {
        var known = knownIds.Select(i => new WorkItemId(i)).ToHashSet();
        return comments
            .Where(c => !string.IsNullOrWhiteSpace(c.Text) && WorkItemIdGuard.IsGrounded(c.Text, known))
            .Select(c =>
            {
                // A comment that names no item belongs to the single item the utterance was about; with several it stays unlinked.
                var ids = Known(c.WorkItemIds, knownIds);
                return c with { WorkItemIds = ids.Count > 0 ? ids : utteranceIds.Count == 1 ? [.. utteranceIds] : [] };
            })
            .ToList();
    }
}
