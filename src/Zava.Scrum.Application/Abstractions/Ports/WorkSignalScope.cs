using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Explicit team source boundaries derived from verified bindings, stored meetings, the board and roster.</summary>
/// <param name="TeamId">The application team.</param>
/// <param name="TeamsGroupId">The bound Microsoft 365 group, not a model-supplied id.</param>
/// <param name="ChannelId">The selected channel, never a chat id.</param>
/// <param name="MeetingIds">This team's stored meeting identifiers.</param>
/// <param name="WorkItemIds">This team's current board identifiers.</param>
/// <param name="RosterObjectIds">Verified roster identities permitted as helpers or update authors.</param>
public sealed record WorkSignalScope(
    string TeamId, string TeamsGroupId, string ChannelId, IReadOnlyList<string> MeetingIds,
    IReadOnlyList<WorkItemId> WorkItemIds, IReadOnlyList<Guid> RosterObjectIds);
