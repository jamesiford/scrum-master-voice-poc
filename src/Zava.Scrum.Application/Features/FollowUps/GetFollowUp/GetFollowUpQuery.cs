using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Application.Features.FollowUps.GetFollowUp;

/// <summary>Reads one follow-up.</summary>
/// <param name="FollowUpId">Follow-up id.</param>
public sealed record GetFollowUpQuery(Guid FollowUpId) : IQuery<FollowUpSnapshot>;
