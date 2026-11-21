namespace Mcm.Scrum.Application.Abstractions.Ports;

/// <summary>The cards the agent sends (SPEC 12).</summary>
public enum CardKind
{
    /// <summary>Morning pre-brief for the human Scrum Master.</summary>
    PreBrief,

    /// <summary>Per-person follow-up with Approve, Edit, Reject.</summary>
    FollowUp,

    /// <summary>Asks an absent member for their update.</summary>
    Absent,

    /// <summary>Blocker escalation to the Scrum Master.</summary>
    Escalation,

    /// <summary>Recap in the meeting chat and team channel.</summary>
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
/// <param name="FollowUpId">The follow-up the Approve, Edit and Reject actions apply to.</param>
/// <param name="MemberId">Recipient member, or <see langword="null"/> for the channel or Scrum Master.</param>
public sealed record TeamsMessage(
    CardKind Kind,
    string TeamId,
    Guid SessionId,
    string DedupeKey,
    string Title,
    string Text,
    IReadOnlyList<CardFact> Facts,
    Guid? FollowUpId = null,
    string? MemberId = null);

/// <summary>Sends cards to Teams.</summary>
public interface ITeamsMessenger
{
    /// <summary>Sends a message once per <see cref="TeamsMessage.DedupeKey"/>.</summary>
    /// <param name="message">The message.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task SendAsync(TeamsMessage message, CancellationToken ct);
}
