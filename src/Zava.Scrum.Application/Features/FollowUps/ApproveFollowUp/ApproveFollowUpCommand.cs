using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Identity;

namespace Zava.Scrum.Application.Features.FollowUps.ApproveFollowUp;

/// <summary>Approves a follow-up. Its approval is what permits Azure DevOps writes (F7).</summary>
/// <param name="FollowUpId">The follow-up.</param>
/// <param name="Decision">Who is approving and how that was proven. A display name alone is refused.</param>
public sealed record ApproveFollowUpCommand(Guid FollowUpId, DecisionContext Decision) : ICommand;
