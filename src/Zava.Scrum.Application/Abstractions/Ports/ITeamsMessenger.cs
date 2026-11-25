namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>The cards the agent sends (SPEC 12).</summary>
public enum CardKind
{
    /// <summary>Morning pre-brief for the human Scrum Master.</summary>
    PreBrief,

    /// <summary>Per-person follow-up with Approve and Reject.</summary>
    FollowUp,

    /// <summary>Asks an absent member for their update.</summary>
    Absent,

    /// <summary>Blocker escalation to the Scrum Master.</summary>
    Escalation,

    /// <summary>A follow-up after a decision: same card without the actions.</summary>
    FollowUpResult,

    /// <summary>Recap in the meeting chat and team channel.</summary>
    Recap,

    /// <summary>Asks a member to help unblock a teammate (M9-R2). No actions: it is a request, not a decision.</summary>
    Unblock,

    /// <summary>Sprint plan for the Scrum Master, with Approve plan and Reject (<c>approve-plan</c> and <c>reject-plan</c> actions).</summary>
    PlanningPack,

    /// <summary>Retrospective actions for the Scrum Master, with Approve all and Reject.</summary>
    RetroActions,

    /// <summary>Sprint review pack for the Scrum Master, with Approve all and Reject.</summary>
    ReviewPack,

    /// <summary>Proposed sprint calendar for the Scrum Master, with Approve and Reject (<c>approve-calendar</c> and <c>reject-calendar</c> actions).</summary>
    SprintCalendar,
}

/// <summary>Which stored conversations a card goes to.</summary>
public enum CardRoute
{
    /// <summary>Every stored conversation (DEMO: whoever installed or messaged the bot).</summary>
    AllConversations,

    /// <summary>The team's recap channel and the meeting chat when the bot is in it; every conversation only as a fallback (M9-R5).</summary>
    Recap,
}

/// <summary>One label and value row on a card.</summary>
/// <param name="Label">The label.</param>
/// <param name="Value">The value.</param>
public sealed record CardFact(string Label, string Value);

/// <summary>A message to send to Teams. Rendering to an Adaptive Card is the adapter's job.</summary>
/// <param name="Kind">Card kind.</param>
/// <param name="TeamId">Team id.</param>
/// <param name="SessionId">The standup.</param>
/// <param name="DedupeKey">Sending the same key twice sends one card (notification handlers must be idempotent).</param>
/// <param name="Title">Card title.</param>
/// <param name="Text">Card body text.</param>
/// <param name="Facts">Rows under the text.</param>
/// <param name="FollowUpId">The follow-up the Approve and Reject actions apply to; for a <see cref="CardKind.PlanningPack"/> card, the planning pack id.</param>
/// <param name="MemberId">Recipient member, or <see langword="null"/> for the channel or Scrum Master.</param>
/// <param name="Route">Where it goes.</param>
/// <param name="RecapChannelKey">The team's recap channel conversation key, used by <see cref="CardRoute.Recap"/>.</param>
/// <param name="CorrelationId">The session's W3C trace id, so an outbox entry can be traced to its audit chain.</param>
public sealed record TeamsMessage(
    CardKind Kind,
    string TeamId,
    Guid SessionId,
    string DedupeKey,
    string Title,
    string Text,
    IReadOnlyList<CardFact> Facts,
    Guid? FollowUpId = null,
    string? MemberId = null,
    CardRoute Route = CardRoute.AllConversations,
    string? RecapChannelKey = null,
    string? CorrelationId = null);

/// <summary>Sends cards to Teams.</summary>
public interface ITeamsMessenger
{
    /// <summary>Sends a message once per <see cref="TeamsMessage.DedupeKey"/>.</summary>
    /// <param name="message">The message.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task SendAsync(TeamsMessage message, CancellationToken ct);

    /// <summary>
    /// Replaces a card that was sent earlier (matched by <see cref="TeamsMessage.DedupeKey"/>), for example to show a decision
    /// and remove the buttons. Does nothing if that card was never sent.
    /// </summary>
    /// <param name="message">The replacement content.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task UpdateAsync(TeamsMessage message, CancellationToken ct);
}
