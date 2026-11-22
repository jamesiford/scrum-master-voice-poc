using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Features.Standups;

/// <summary>A session in the Sessions list.</summary>
/// <param name="Id">Session id.</param>
/// <param name="TeamId">Team id.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="CreatedAt">When it was prepared.</param>
/// <param name="FlaggedItems">Number of open items with at least one flag.</param>
/// <param name="Kind">The ceremony.</param>
public sealed record SessionSummaryDto(Guid Id, string TeamId, StandupStatus Status, DateTimeOffset CreatedAt, int FlaggedItems, CeremonyKind Kind = CeremonyKind.DailyScrum);
