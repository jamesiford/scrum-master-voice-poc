using Microsoft.Extensions.Logging;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Teams;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Reports;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Reports.AnswerCopilotQuestion;

/// <summary>Resolves membership, validates the asker assertion, reads through OBO and returns only code-selected facts.</summary>
internal sealed class AnswerCopilotQuestionHandler(
    ITeamResolver resolver, IIdentityPolicy identity, IUserAssertionAccessor assertions,
    IApproverTokenSource tokens, IDelegatedWorkItemReader reader, IMemberRepository members,
    ISessionRepository sessions, IClock clock, ICopilotPhraser phraser,
    ILogger<AnswerCopilotQuestionHandler> logger) : IRequestHandler<AnswerCopilotQuestionQuery, Result<CopilotAnswerDto>>
{
    /// <summary>Answers without writes or service-identity read fallbacks.</summary>
    /// <param name="request">Verified asker and explicit skill/team choice.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A bounded answer, or the precise refusal.</returns>
    public async Task<Result<CopilotAnswerDto>> Handle(AnswerCopilotQuestionQuery request, CancellationToken ct)
    {
        var assertion = assertions.Current;
        if (request.Asker.TenantId != identity.TenantId || assertion is null ||
            assertion.ObjectId != request.Asker.ObjectId || assertion.TenantId != request.Asker.TenantId)
        {
            return Result<CopilotAnswerDto>.Failure(CopilotErrors.SignInRequired);
        }

        var resolved = await resolver.ForUserAsync(request.Asker.ObjectId, request.ChosenTeamId, ct);
        if (!resolved.IsSuccess)
        {
            if (resolved.Error!.Code == "team.ambiguous")
            {
                var choices = await resolver.ListForCallerAsync(new CallerContext(request.Asker.ObjectId, false), false, ct);
                return Result<CopilotAnswerDto>.Failure(Error.Conflict("team.ambiguous",
                    $"Which team? {string.Join(", ", choices.Select(t => $"{t.Name} ({t.Id})"))}. Repeat your question as 'team <id>: <question>'. No sprint data was read."));
            }

            return Result<CopilotAnswerDto>.Failure(resolved.Error!.Code == TeamErrors.NoTeam.Code ? CopilotErrors.NoTeam : resolved.Error);
        }

        var team = resolved.Value;
        var access = await tokens.ExchangeAsync(assertion, ct);
        if (!access.IsSuccess)
        {
            return Result<CopilotAnswerDto>.Failure(access.Error!);
        }

        var sprint = await reader.GetCurrentSprintAsync(team, access.Value, ct);
        if (!sprint.IsSuccess)
        {
            return Result<CopilotAnswerDto>.Failure(sprint.Error!);
        }

        var roster = (await members.ListAsync(team.Id, ct)).Where(m => m.TeamId == team.Id && m.EntraObjectId == request.Asker.ObjectId).ToList();
        var member = roster.Count == 1 ? roster[0] : null;
        if (request.Skill is CopilotSkill.MyItems or CopilotSkill.Commitments &&
            (member is null || request.Skill == CopilotSkill.MyItems && string.IsNullOrWhiteSpace(member.AdoUniqueName)))
        {
            return Result<CopilotAnswerDto>.Failure(CopilotErrors.RosterRequired);
        }

        var history = request.Skill is CopilotSkill.Commitments or CopilotSkill.StandupStatus
            ? await sessions.ListAsync(ct) : [];
        var facts = CopilotFactsBuilder.Build(team, request.Skill, sprint.Value, member, history, clock.UtcNow);
        var intro = await phraser.PhraseAsync(facts.Summary, ct);
        var fromModel = CopilotPhrasing.IsSafe(intro);
        if (intro is not null && !fromModel)
        {
            logger.LogWarning("Discarded ungrounded Copilot phrasing for team {TeamId}", team.Id);
        }

        var fallback = intro is null
            ? "The language model is unavailable; here are the code-computed facts from your Azure DevOps access."
            : "I discarded ungrounded AI phrasing; here are the code-computed facts from your Azure DevOps access.";
        var text = string.Join("\n", new[] { fromModel ? intro!.Trim() : fallback, facts.Summary }.Concat(facts.Lines));
        var links = facts.ItemIds.Select(id => $"{team.Ado.Org.TrimEnd('/')}/{Uri.EscapeDataString(team.Ado.Project)}/_workitems/edit/{id.Value}").ToList();
        return Result<CopilotAnswerDto>.Success(new(team.Id, text, links, fromModel));
    }
}
