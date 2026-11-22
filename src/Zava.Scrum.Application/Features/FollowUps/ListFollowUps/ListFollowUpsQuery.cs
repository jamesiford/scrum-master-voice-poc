using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Application.Features.FollowUps.ListFollowUps;

/// <summary>Lists follow-ups for a team.</summary>
/// <param name="TeamId">Team id.</param>
/// <param name="Status">Optional status filter.</param>
public sealed record ListFollowUpsQuery(string TeamId, FollowUpStatus? Status = null) : IQuery<IReadOnlyList<FollowUpSnapshot>>;
