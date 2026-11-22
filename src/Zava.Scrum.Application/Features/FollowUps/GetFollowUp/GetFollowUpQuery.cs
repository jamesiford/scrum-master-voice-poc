using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Domain.FollowUps;

namespace Mcm.Scrum.Application.Features.FollowUps.GetFollowUp;

/// <summary>Reads one follow-up.</summary>
/// <param name="FollowUpId">Follow-up id.</param>
public sealed record GetFollowUpQuery(Guid FollowUpId) : IQuery<FollowUpSnapshot>;
