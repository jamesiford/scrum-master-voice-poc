using System.Text.RegularExpressions;

namespace Mcm.Scrum.Domain.Voice;

/// <summary>What an utterance is trying to do, judged in the context of whose turn it is.</summary>
public enum UtteranceIntent
{
    /// <summary>The floor holder giving (part of) their update, or nothing special.</summary>
    Update,

    /// <summary>Someone says the speaker (or a named teammate) stepped away or needs a moment.</summary>
    StepAway,

    /// <summary>One member offers to give another's update.</summary>
    ProxyUpdate,

    /// <summary>A member asks what they are supposed to report on.</summary>
    AskWhatToReport,

    /// <summary>A member asks for a fact about a work item.</summary>
    AskItemInfo,

    /// <summary>A member corrects something they said earlier.</summary>
    Correction,

    /// <summary>A member who does not hold the floor starts giving their own update.</summary>
    OutOfTurnUpdate,

    /// <summary>A member says they are back.</summary>
    BackNow,

    /// <summary>Anything else from someone who does not hold the floor.</summary>
    Chatter,
}

/// <summary>A roster entry the classifier can recognise in speech.</summary>
/// <param name="MemberId">Member id.</param>
/// <param name="DisplayName">Display name.</param>
public sealed record NameRef(string MemberId, string DisplayName);

/// <summary>The result of classifying an utterance.</summary>
/// <param name="Intent">What it is.</param>
/// <param name="TargetMemberId">The member it concerns (who is away, whose update is given), if any.</param>
/// <param name="ByMemberId">For a proxy, the member who will speak for the target.</param>
public sealed record Interpretation(UtteranceIntent Intent, string? TargetMemberId = null, string? ByMemberId = null);

/// <summary>
/// Classifies speech with plain rules so it adds no latency and is testable (rule 3). A model never decides who is away or whose
/// update is being given; it only phrases the agent's reply.
/// </summary>
public static partial class UtteranceClassifier
{
    /// <summary>Classifies an utterance.</summary>
    /// <param name="text">What was said.</param>
    /// <param name="speakerId">Who said it.</param>
    /// <param name="holderId">Whose turn it is, if anyone's.</param>
    /// <param name="roster">Everyone in the standup, to recognise names.</param>
    /// <returns>The interpretation.</returns>
    public static Interpretation Classify(string? text, string speakerId, string? holderId, IReadOnlyList<NameRef> roster)
    {
        ArgumentNullException.ThrowIfNull(roster);
        ArgumentException.ThrowIfNullOrEmpty(speakerId);
        var isHolder = speakerId == holderId;
        if (string.IsNullOrWhiteSpace(text))
        {
            return new Interpretation(isHolder ? UtteranceIntent.Update : UtteranceIntent.Chatter);
        }

        var named = Named(text, speakerId, roster);
        var other = named.FirstOrDefault(m => m != holderId) ?? named.FirstOrDefault();

        if (Proxy().IsMatch(text))
        {
            return ClassifyProxy(text, speakerId, holderId, isHolder, named, other);
        }

        if (Away().IsMatch(text))
        {
            var target = isHolder && named.Count == 0 ? speakerId : named.Count > 0 ? other : Pronoun().IsMatch(text) ? holderId : isHolder ? speakerId : null;
            if (target is not null)
            {
                return new Interpretation(UtteranceIntent.StepAway, target);
            }
        }

        if (WhatToReport().IsMatch(text))
        {
            return new Interpretation(UtteranceIntent.AskWhatToReport, speakerId);
        }

        if (Correction().IsMatch(text))
        {
            return new Interpretation(UtteranceIntent.Correction, speakerId);
        }

        if (ItemQuestionDetector.IsItemQuestion(text))
        {
            return new Interpretation(UtteranceIntent.AskItemInfo, speakerId);
        }

        if (Back().IsMatch(text) && !isHolder)
        {
            return new Interpretation(UtteranceIntent.BackNow, speakerId);
        }

        if (isHolder)
        {
            return new Interpretation(UtteranceIntent.Update, speakerId);
        }

        return new Interpretation(OwnUpdate().IsMatch(text) ? UtteranceIntent.OutOfTurnUpdate : UtteranceIntent.Chatter, speakerId);
    }

    private static Interpretation ClassifyProxy(string text, string speakerId, string? holderId, bool isHolder, List<string> named, string? other)
    {
        // "Maya will give my update": the holder hands over to someone else.
        if (isHolder && other is not null && HandOver().IsMatch(text))
        {
            return new Interpretation(UtteranceIntent.ProxyUpdate, speakerId, other);
        }

        // "I'll give Admin's update" / "I'll do his update": the speaker covers a named member, or the member whose turn it is.
        var target = named.Count > 0 ? other : Pronoun().IsMatch(text) || !isHolder ? holderId : null;
        return target is null || target == speakerId
            ? new Interpretation(isHolder ? UtteranceIntent.Update : UtteranceIntent.Chatter, speakerId)
            : new Interpretation(UtteranceIntent.ProxyUpdate, target, speakerId);
    }

    // A name matches when any word of the display name is spoken, so "Admin" matches "Admin" and "Maya" matches "Maya Lin".
    private static List<string> Named(string text, string speakerId, IReadOnlyList<NameRef> roster)
    {
        var words = Words().Matches(text).Select(m => m.Value.ToLowerInvariant()).ToHashSet();
        return roster
            .Where(r => r.MemberId != speakerId && r.DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(p => p.Length > 1 && words.Contains(p.ToLowerInvariant().TrimEnd('.'))))
            .Select(r => r.MemberId)
            .ToList();
    }

    [GeneratedRegex(@"[A-Za-z']+")]
    private static partial Regex Words();

    [GeneratedRegex(@"\b(he|she|they|him|her|them|his|hers|their)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Pronoun();

    [GeneratedRegex(@"\b(i['’]?ll|i will|let me|i can|i['’]?m going to|i['’]?d be happy to|allow me to)\s+(give|do|cover|deliver|share|provide|say|report|take|present)\b.{0,30}\bupdate|\bon behalf of\b|\b(asked|told)\s+me\s+to\s+(give|share|report|cover|do)|\b(giving|covering|doing|delivering)\s+(\w+['’]s|his|her|their)\s+update|\b(will|can|could|is going to)\s+(give|do|cover|deliver|share)\s+my\s+update|\bgive\s+my\s+update\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Proxy();

    [GeneratedRegex(@"\bmy\s+update\b|\bfor\s+me\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HandOver();

    [GeneratedRegex(@"\b(bathroom|restroom|toilet|stepped away|step away|stepped out|step out|ran out for|run out for|had to (go|run|leave|step out)|be right back|brb|back in (a|an|\d+|few|couple|five|ten|fifteen)\b|(will|'ll|’ll) be back (in|shortly|soon)|take a call|got a call|one (sec|second|minute|moment)|give me a (minute|sec|second|moment)|hold on a (sec|second|minute|moment)|can (we|you|i) (come|circle) back (to|with) me|come back to me|skip me)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Away();

    [GeneratedRegex(@"\bwhat\s+(do|should|am|would)\s+(i|you want me)\b.{0,25}\b(report|update|cover|talk|give|say|speak|share|supposed|meant)|\bwhat\s+(am|are)\s+(i|you)\s+(supposed|meant|looking|asking)|\bwhat('s| is)\s+(on\s+)?my\s+(list|plate|agenda)|\bwhich\s+(items|stories|tasks|bugs)\s+(do|should)\s+i|\bwhat\s+(updates|items)\s+(do|should)|\bremind me what\b|\bwhat do you need (from me|to know)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WhatToReport();

    [GeneratedRegex(@"\b(correction|i misspoke|scratch that|take that back|never ?mind (that|what i said)|that['’]?s (wrong|not right|incorrect|not quite right)|i meant|actually,? no|no,? wait|wait,? no|let me correct|strike that|disregard (that|what))\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Correction();

    [GeneratedRegex(@"\b(i['’]?m|i am)\s+back\b|\bi['’]?m here now\b|\bsorry,? i['’]?m back\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Back();

    [GeneratedRegex(@"\b(yesterday|this morning|today)\s+i\b|\bmy update\b|\bi['’]?m\s+(blocked|working on|waiting)\b|\bi\s+(finished|worked on|shipped|merged)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OwnUpdate();
}
