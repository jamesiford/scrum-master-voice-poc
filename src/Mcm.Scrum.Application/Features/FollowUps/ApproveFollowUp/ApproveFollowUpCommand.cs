using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.FollowUps.ApproveFollowUp;

/// <summary>Approves a follow-up. Its approval is what permits Azure DevOps writes (F7).</summary>
/// <param name="FollowUpId">The follow-up.</param>
/// <param name="Approver">Display name of the person approving.</param>
public sealed record ApproveFollowUpCommand(Guid FollowUpId, string Approver) : ICommand;
