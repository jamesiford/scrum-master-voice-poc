using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Standups;
using Zava.Scrum.Application.Features.Teams;
using Zava.Scrum.Domain.Ceremonies;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Flags;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Features.Retrospectives.PrepareRetrospective;

/// <summary>
/// Builds the session the voice gateway runs the retrospective on. The opening is the same factual draft the Retrospective page shows
/// (<see cref="RetroPackBuilder"/>), so every figure comes from code (rule 3). Handlers do not call handlers, so it shares the Domain builder.
/// </summary>
internal sealed class PrepareRetrospectiveHandler(
    ITeamRepository teams,
    IMemberRepository members,
    IWorkItemReader reader,
    IIterationReader iterations,
    ISessionRepository sessions,
    IClock clock) : IRequestHandler<PrepareRetrospectiveCommand, Result<Guid>>
{
    private readonly PreBriefBuilder _builder = new(FlagEvaluator.CreateDefault());

    public async Task<Result<Guid>> Handle(PrepareRetrospectiveCommand request, CancellationToken ct)
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
        var previous = (await sessions.ListAsync(ct)).Where(s => s.TeamId == team.Id).ToList();
        var pack = RetroPackBuilder.Build(sprint.Value, previous, roster, await iterations.GetNextIterationPathAsync(team, ct));
        var opening = string.Join("; ", pack.Facts.Select(f => $"{f.Label}: {f.Value}"));
        var brief = _builder.Build(team, roster, sprint.Value, now) with { Summary = opening, TeamRisks = [] };

        var session = StandupSession.Prepare(Guid.NewGuid(), team.Id, now, brief, CeremonyKind.Retrospective);
        session.ClearDomainEvents();
        await sessions.SaveAsync(session, ct);
        return Result<Guid>.Success(session.Id);
    }
}
