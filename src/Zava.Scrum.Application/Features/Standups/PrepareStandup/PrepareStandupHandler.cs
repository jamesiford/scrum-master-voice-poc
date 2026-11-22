using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Teams;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Flags;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Features.Standups.PrepareStandup;

/// <summary>
/// Builds facts in code, asks the Backlog and Risk agent only for phrasing, and drops any text that
/// mentions a work item id outside the current sprint (rules 2 and 3).
/// </summary>
internal sealed class PrepareStandupHandler(
    ITeamRepository teams,
    IMemberRepository members,
    IWorkItemReader reader,
    IBacklogRiskAgent agent,
    ISessionRepository sessions,
    IPublisher publisher,
    IClock clock) : IRequestHandler<PrepareStandupCommand, Result<Guid>>
{
    private readonly PreBriefBuilder _builder = new(FlagEvaluator.CreateDefault());

    public async Task<Result<Guid>> Handle(PrepareStandupCommand request, CancellationToken ct)
    {
        var team = await teams.GetAsync(request.TeamId, ct);
        if (team is null)
        {
            return Result<Guid>.Failure(TeamErrors.NotFound(request.TeamId));
        }

        var roster = await members.ListAsync(team.Id, ct);
        if (!roster.Any(m => m.Active))
        {
            return Result<Guid>.Failure(StandupErrors.RosterEmpty);
        }

        var sprint = await reader.GetCurrentSprintAsync(team, ct);
        if (!sprint.IsSuccess)
        {
            return Result<Guid>.Failure(sprint.Error!);
        }

        var now = clock.UtcNow;
        var open = sprint.Value.Items.Where(i => !i.IsDone).Select(i => i.Id.Value).ToHashSet();
        var previous = (await sessions.ListAsync(ct)).Where(s => s.TeamId == team.Id).ToList();
        var brief = _builder.Build(team, roster, sprint.Value, now, CommitmentCarryOver.Collect(previous, open));
        brief = await ApplySuggestionsAsync(brief, sprint.Value, ct);

        var session = StandupSession.Prepare(Guid.NewGuid(), team.Id, now, brief);
        await sessions.SaveAsync(session, ct);
        await session.PublishEventsAsync(publisher, ct);
        return Result<Guid>.Success(session.Id);
    }

    private async Task<PreBrief> ApplySuggestionsAsync(PreBrief brief, Domain.Work.SprintSnapshot sprint, CancellationToken ct)
    {
        var suggestion = await agent.SuggestAsync(brief, ct);
        if (suggestion is null)
        {
            return brief;
        }

        var known = sprint.Items.Select(i => i.Id).ToHashSet();
        var knownMembers = brief.Members.Select(m => m.MemberId).ToHashSet();
        var questions = suggestion.QuestionsByMemberId
            .Where(q => knownMembers.Contains(q.Key) && !string.IsNullOrWhiteSpace(q.Value) && WorkItemIdGuard.IsGrounded(q.Value, known))
            .ToDictionary(q => q.Key, q => q.Value);
        var summary = WorkItemIdGuard.IsGrounded(suggestion.Summary, known) && !string.IsNullOrWhiteSpace(suggestion.Summary)
            ? suggestion.Summary
            : null;

        return PreBriefBuilder.WithSuggestions(brief, summary, questions);
    }
}
