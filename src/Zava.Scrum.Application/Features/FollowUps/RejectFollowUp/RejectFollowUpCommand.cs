using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Identity;

namespace Zava.Scrum.Application.Features.FollowUps.RejectFollowUp;

/// <summary>Rejects a follow-up. Its changes are never written.</summary>
/// <param name="FollowUpId">The follow-up.</param>
/// <param name="Decision">Who is rejecting and how that was proven.</param>
public sealed record RejectFollowUpCommand(Guid FollowUpId, DecisionContext Decision) : ICommand;
