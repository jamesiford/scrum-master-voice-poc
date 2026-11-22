using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Domain.FollowUps;

namespace Mcm.Scrum.Application.Features.FollowUps.ListFollowUps;

/// <summary>Lists follow-ups for a team.</summary>
/// <param name="TeamId">Team id.</param>
/// <param name="Status">Optional status filter.</param>
public sealed record ListFollowUpsQuery(string TeamId, FollowUpStatus? Status = null) : IQuery<IReadOnlyList<FollowUpSnapshot>>;
