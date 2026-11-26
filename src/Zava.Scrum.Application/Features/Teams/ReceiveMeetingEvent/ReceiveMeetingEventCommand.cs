using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Teams.ReceiveMeetingEvent;

/// <summary>Receives a Teams system event scoped by tenant, bound conversation and stored meeting, never by an event sender.</summary>
/// <param name="TenantId">The tenant on the authenticated Teams activity.</param>
/// <param name="ConversationId">The conversation delivering the event.</param>
/// <param name="MeetingId">The opaque meeting id from channelData.meeting.id, not activity.value.Id.</param>
/// <param name="JoinUrl">The join URL from typed meeting details, used to find the stored session.</param>
/// <param name="Kind">Start or end.</param>
public sealed record ReceiveMeetingEventCommand(
    string? TenantId,
    string? ConversationId,
    string? MeetingId,
    string? JoinUrl,
    MeetingEventKind Kind) : ICommand;
