using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Domain.FollowUps;

/// <summary>Raised when a person approves a follow-up. A handler applies its changes to Azure DevOps.</summary>
/// <param name="FollowUpId">The follow-up.</param>
/// <param name="OccurredAt">When it was approved.</param>
public sealed record FollowUpApproved(Guid FollowUpId, DateTimeOffset OccurredAt) : IDomainEvent;
