namespace Zava.Scrum.Domain.Standups;

/// <summary>Teams lifecycle metadata correlated to a ceremony's stored join URL, not meeting content.</summary>
/// <param name="MeetingId">The opaque id from Teams channel data.</param>
/// <param name="ConversationId">The bound conversation that delivered the event.</param>
/// <param name="StartedAt">When the meeting start event was received, if delivered.</param>
/// <param name="EndedAt">When the meeting end event was received, if delivered.</param>
public sealed record MeetingLifecycle(string MeetingId, string ConversationId, DateTimeOffset? StartedAt, DateTimeOffset? EndedAt);
