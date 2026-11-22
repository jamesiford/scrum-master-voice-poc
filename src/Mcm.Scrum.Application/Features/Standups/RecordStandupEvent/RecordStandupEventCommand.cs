using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Features.Standups.RecordStandupEvent;

/// <summary>Records one event. Work item ids that are not in the pre-brief are dropped (rule 2).</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="Type">Event kind.</param>
/// <param name="MemberId">Member concerned, if any.</param>
/// <param name="Text">What happened.</param>
/// <param name="WorkItemIds">Work items referred to.</param>
/// <param name="Category">For a retrospective note, its column.</param>
/// <param name="OwnerMemberId">For a retrospective action, its owner.</param>
public sealed record RecordStandupEventCommand(
    Guid SessionId,
    StandupEventType Type,
    string? MemberId,
    string Text,
    IReadOnlyList<int>? WorkItemIds = null,
    RetroCategory? Category = null,
    string? OwnerMemberId = null) : ICommand;
