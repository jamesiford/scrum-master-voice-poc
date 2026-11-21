using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.FollowUps.RejectFollowUp;

/// <summary>Rejects a follow-up. Nothing is written to Azure DevOps.</summary>
/// <param name="FollowUpId">The follow-up.</param>
/// <param name="Rejecter">Display name of the person rejecting.</param>
public sealed record RejectFollowUpCommand(Guid FollowUpId, string Rejecter) : ICommand;
