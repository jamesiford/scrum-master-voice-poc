using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Application.Features.FollowUps.ApproveFollowUp;

/// <summary>Approves a follow-up. Its approval is what permits Azure DevOps writes (F7).</summary>
/// <param name="FollowUpId">The follow-up.</param>
/// <param name="Approver">Display name of the person approving.</param>
/// <param name="Channel">Where they approved; written into the Azure DevOps attribution.</param>
public sealed record ApproveFollowUpCommand(Guid FollowUpId, string Approver, ApprovalChannel Channel = ApprovalChannel.Teams) : ICommand;
