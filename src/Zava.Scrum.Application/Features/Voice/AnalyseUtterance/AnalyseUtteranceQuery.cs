using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Voice;

namespace Zava.Scrum.Application.Features.Voice.AnalyseUtterance;

/// <summary>
/// Reads one spoken utterance: what the speaker is doing, which work items it is about, and what kind of comment it is.
/// A query: it records nothing; the caller acts on the analysis.
/// </summary>
/// <param name="SessionId">Session id.</param>
/// <param name="SpeakerId">Who said it.</param>
/// <param name="HolderId">Whose turn it is, if anyone's.</param>
/// <param name="Text">What was said.</param>
/// <param name="Roster">Everyone in the ceremony; <see langword="null"/> uses the members of the session's pre-brief.</param>
/// <param name="Asked">The question the agent just asked the speaker, if any.</param>
/// <param name="FocusItemId">The item that question was about.</param>
public sealed record AnalyseUtteranceQuery(
    Guid SessionId,
    string SpeakerId,
    string? HolderId,
    string Text,
    IReadOnlyList<NameRef>? Roster,
    string? Asked = null,
    int? FocusItemId = null) : IQuery<UtteranceAnalysis>;
