using System.Text.RegularExpressions;

namespace Mcm.Scrum.Domain.Standups;

/// <summary>A retrospective event to record.</summary>
/// <param name="Type">Either <see cref="StandupEventType.RetroItem"/> or <see cref="StandupEventType.ActionItem"/>.</param>
/// <param name="Text">The note or action.</param>
/// <param name="Category">The column, for a note; <see langword="null"/> when unclear.</param>
/// <param name="OwnerMemberId">The owner, for an action.</param>
public sealed record RetroNote(StandupEventType Type, string Text, RetroCategory? Category, string? OwnerMemberId);

/// <summary>What a retrospective utterance changes.</summary>
/// <param name="Notes">Events to record.</param>
/// <param name="RetractedEventIds">Earlier events replaced by a correction or by the answer to a clarifying question.</param>
/// <param name="Question">What the agent asks next, or <see langword="null"/>.</param>
public sealed record RetroInterpretation(IReadOnlyList<RetroNote> Notes, IReadOnlyList<Guid> RetractedEventIds, string? Question);

/// <summary>
/// Turns what a member said in a retrospective into notes, action items and at most one question. Pure and table-testable: the
/// model never decides a category, an owner or a count (rule 3).
/// </summary>
public static partial class RetroInterpreter
{
    private const int MinWords = 3;

    private static readonly string[] NoAction = ["nothing", "pass", "no action", "none from me", "i'm good", "no actions"];

    /// <summary>Interprets a Round turn: the member's notes, or the answer to "start, stop or keep?".</summary>
    /// <param name="memberId">The speaker.</param>
    /// <param name="utterance">What they said.</param>
    /// <param name="answering">True when the agent just asked the clarifying question.</param>
    /// <param name="correction">True when the member is correcting themselves.</param>
    /// <param name="active">The session's active events.</param>
    /// <returns>What to record, retract and ask.</returns>
    public static RetroInterpretation Round(string memberId, string utterance, bool answering, bool correction, IReadOnlyList<StandupEvent> active)
    {
        ArgumentNullException.ThrowIfNull(active);
        var mine = active.Where(e => e.Type == StandupEventType.RetroItem && e.MemberId == memberId).ToList();
        var retract = new List<Guid>();
        var notes = new List<RetroNote>();

        // "We should stop X. Scratch that, we should keep X": only what follows the last marker counts. A correction made inside one
        // utterance has nothing recorded yet to retract, so it must not retract an earlier note.
        if (!answering && CorrectionMarker().Matches(utterance) is { Count: > 0 } found)
        {
            var marker = found[^1];
            var inline = utterance[..marker.Index].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= MinWords;
            utterance = utterance[(marker.Index + marker.Length)..];
            correction = correction && !inline;
        }

        var pending = mine.FirstOrDefault(e => e.Category is null);
        if (answering && pending is not null && RetroItemClassifier.Classify(utterance) is { } answer)
        {
            retract.Add(pending.Id);
            notes.Add(new RetroNote(StandupEventType.RetroItem, pending.Text, answer, null));
            return new RetroInterpretation(notes, retract, null);
        }

        if (answering)
        {
            // An answer that is not a category is not asked again: the note stays unsorted rather than looping.
            return new RetroInterpretation([], [], null);
        }

        if (correction && mine.LastOrDefault() is { } last)
        {
            retract.Add(last.Id);
            mine.Remove(last);
        }

        var room = Math.Max(0, RetroAgenda.MaxItemsPerMember - mine.Count);
        foreach (var text in RetroItemClassifier.Split(utterance).Take(room))
        {
            notes.Add(new RetroNote(StandupEventType.RetroItem, text, RetroItemClassifier.Classify(text), null));
        }

        var unclear = mine.Where(e => e.Category is null && !retract.Contains(e.Id)).Any() || notes.Any(n => n.Category is null);
        return new RetroInterpretation(notes, retract, unclear ? RetroItemClassifier.ClarifyingQuestion : null);
    }

    /// <summary>Interprets an Actions turn: one action item and its owner, or the answer to "does that owner work?".</summary>
    /// <param name="memberId">The speaker.</param>
    /// <param name="utterance">What they said.</param>
    /// <param name="answering">True when the agent just asked them to confirm the owner.</param>
    /// <param name="roster">The members.</param>
    /// <param name="active">The session's active events.</param>
    /// <returns>What to record, retract and ask.</returns>
    public static RetroInterpretation Action(string memberId, string utterance, bool answering, IReadOnlyList<MemberBrief> roster, IReadOnlyList<StandupEvent> active)
    {
        ArgumentNullException.ThrowIfNull(roster);
        ArgumentNullException.ThrowIfNull(active);
        var last = active.LastOrDefault(e => e.Type == StandupEventType.ActionItem && e.MemberId == memberId);
        if (answering)
        {
            // Only a named teammate changes the owner; "yes" or "no" leaves it as proposed.
            var named = last is null ? null : UnblockerResolver.Resolve(utterance, roster, last.OwnerMemberId ?? string.Empty);
            return named is null || last is null
                ? new RetroInterpretation([], [], null)
                : new RetroInterpretation([new RetroNote(StandupEventType.ActionItem, last.Text, null, named)], [last.Id], null);
        }

        var text = Lead().Replace(utterance.Trim(), string.Empty).Trim(' ', '.', ',', '!', '?');
        if (text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 3 || NoAction.Any(n => text.Contains(n, StringComparison.OrdinalIgnoreCase) && text.Length < 24))
        {
            return new RetroInterpretation([], [], null);
        }

        var owner = UnblockerResolver.Resolve(utterance, roster, string.Empty) ?? memberId;
        var title = char.ToUpperInvariant(text[0]) + text[1..];
        var ownerName = roster.FirstOrDefault(m => m.MemberId == owner)?.DisplayName ?? owner;
        return new RetroInterpretation([new RetroNote(StandupEventType.ActionItem, title, null, owner)], [], $"I have that as \"{title}\", owned by {ownerName}. Does that work?");
    }

    [GeneratedRegex(@"\b(?:correction|i misspoke|scratch that|take that back|strike that|let me correct|i meant|actually,? no|no,? wait|wait,? no)\b[,:\s]*", RegexOptions.IgnoreCase)]
    private static partial Regex CorrectionMarker();

    [GeneratedRegex(@"^(?:okay|ok|so|um|uh|well|my action is|an action is|the action is|action:)\b[,:\s]*", RegexOptions.IgnoreCase)]
    private static partial Regex Lead();
}
