using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Voice;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Everything the decision model may use to read one utterance. All of it is decided by code; the model only interprets speech.</summary>
/// <param name="Text">What was said, transcribed.</param>
/// <param name="SpeakerId">Who said it.</param>
/// <param name="HolderId">Whose turn it is, if anyone's.</param>
/// <param name="Roster">Everyone in the ceremony, so names resolve to ids.</param>
/// <param name="Items">The open sprint items the model may link to, so "the retry story" resolves to an id.</param>
/// <param name="Asked">The question the agent just asked, so a short answer is read in context.</param>
/// <param name="FocusItemId">The item that question was about.</param>
public sealed record UtteranceContext(
    string Text,
    string SpeakerId,
    string? HolderId,
    IReadOnlyList<NameRef> Roster,
    IReadOnlyList<BriefItem> Items,
    string? Asked,
    int? FocusItemId);
