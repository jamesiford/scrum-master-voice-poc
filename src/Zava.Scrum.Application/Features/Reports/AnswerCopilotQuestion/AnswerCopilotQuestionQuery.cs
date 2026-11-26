using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Identity;
using Zava.Scrum.Domain.Reports;

namespace Zava.Scrum.Application.Features.Reports.AnswerCopilotQuestion;

/// <summary>Answers a read-only skill as the verified asker; an explicit team choice is required for multiple memberships.</summary>
/// <param name="Asker">Identity from the authenticated channel activity.</param>
/// <param name="Skill">The bounded read-only skill.</param>
/// <param name="ChosenTeamId">The user's explicit team choice, not a conversation binding.</param>
public sealed record AnswerCopilotQuestionQuery(ActorIdentity Asker, CopilotSkill Skill, string? ChosenTeamId = null)
    : IQuery<CopilotAnswerDto>, IResolvedTeamRequest<Result<CopilotAnswerDto>>
{
    /// <inheritdoc />
    public string? GetResolvedTeamId(Result<CopilotAnswerDto> response) =>
        response.IsSuccess ? response.Value.TeamId : ChosenTeamId;
}
