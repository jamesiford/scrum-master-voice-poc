using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Domain.FollowUps;

/// <summary>Raised when a person approves a follow-up. A handler applies its changes to Azure DevOps.</summary>
/// <param name="FollowUpId">The follow-up.</param>
/// <param name="OccurredAt">When it was approved.</param>
public sealed record FollowUpApproved(Guid FollowUpId, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Raised when a person rejects a follow-up. A handler updates its card; nothing is written.</summary>
/// <param name="FollowUpId">The follow-up.</param>
/// <param name="OccurredAt">When it was rejected.</param>
public sealed record FollowUpRejected(Guid FollowUpId, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Raised when an approved follow-up has been written to Azure DevOps. A handler updates its card.</summary>
/// <param name="FollowUpId">The follow-up.</param>
/// <param name="OccurredAt">When it was applied.</param>
public sealed record FollowUpApplied(Guid FollowUpId, DateTimeOffset OccurredAt) : IDomainEvent;
