using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.FollowUps.ApplyApprovedChanges;

/// <summary>Writes an approved follow-up's changes to Azure DevOps. Refuses unless approved (F7). Safe to retry.</summary>
/// <param name="FollowUpId">The follow-up.</param>
public sealed record ApplyApprovedChangesCommand(Guid FollowUpId) : ICommand;
