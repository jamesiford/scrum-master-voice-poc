using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Retrospectives.CreateRetroOutcome;

/// <summary>
/// Turns an ended retrospective into a Scrum Master follow-up with one <c>CreateWorkItem</c> change per action, and posts the summary.
/// Nothing is written to Azure DevOps until the follow-up is approved (F7). Idempotent: a session that already has a follow-up is left alone.
/// </summary>
/// <param name="SessionId">The retrospective session.</param>
public sealed record CreateRetroOutcomeCommand(Guid SessionId) : ICommand;
