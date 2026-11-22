using System.Text.Json;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Teams;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Planning;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Features.Planning.PreparePlanning;

/// <summary>
/// Computes the proposal in Domain code, lets a model phrase only the draft goal, and stores the pack. A goal that names an item outside the
/// sprint and backlog (rule 2) or a number the proposal does not contain (rule 3) is dropped for the code fallback.
/// </summary>
internal sealed class PreparePlanningHandler(
    ITeamRepository teams,
    IMemberRepository members,
    IWorkItemReader sprintReader,
    IBacklogReader backlogReader,
    IIterationReader iterations,
    IKpiHistorySource history,
    IDraftingAgent agent,
    IPlanningPackRepository packs,
    IPublisher publisher,
    IClock clock) : IRequestHandler<PreparePlanningCommand, Result<Guid>>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<Result<Guid>> Handle(PreparePlanningCommand request, CancellationToken ct)
    {
        var team = await teams.GetAsync(request.TeamId, ct);
        if (team is null)
        {
            return Result<Guid>.Failure(TeamErrors.NotFound(request.TeamId));
        }

        var sprint = await sprintReader.GetCurrentSprintAsync(team, ct);
        if (!sprint.IsSuccess)
        {
            return Result<Guid>.Failure(sprint.Error!);
        }

        var backlog = await backlogReader.GetBacklogAsync(team, ct);
        if (!backlog.IsSuccess)
        {
            return Result<Guid>.Failure(backlog.Error!);
        }

        var roster = await members.ListAsync(team.Id, ct);
        var kpis = await history.GetAsync(team.Id, ct);
        var completed = kpis.Sprints.OrderBy(s => s.End).Select(s => s.CompletedPoints).ToList();
        var proposal = PlanningPlanner.Propose(sprint.Value, backlog.Value, roster, completed);
        var next = await iterations.GetNextIterationPathAsync(team, ct);

        var known = sprint.Value.Items.Concat(backlog.Value).Select(i => i.Id).ToHashSet();
        var (goal, fromModel) = await GoalAsync(proposal, known, ct);
        var pack = PlanningPack.Prepare(Guid.NewGuid(), team.Id, clock.UtcNow, sprint.Value.IterationPath, next, kpis.Illustrative, proposal, goal, fromModel);
        await packs.SaveAsync(pack, ct);
        await pack.PublishEventsAsync(publisher, ct);
        return Result<Guid>.Success(pack.Id);
    }

    private async Task<(string Goal, bool FromModel)> GoalAsync(PlanningProposal proposal, IReadOnlySet<Domain.Work.WorkItemId> known, CancellationToken ct)
    {
        var facts = new { selected = proposal.Selected.Select(s => new { s.WorkItemId, s.Title, s.Points, source = s.Source.ToString() }), proposal.TotalPoints };
        var reply = await agent.DraftJsonAsync("planning-goal", JsonSerializer.Serialize(facts, Json), ct);
        var goal = Goal(reply);
        return goal is not null && WorkItemIdGuard.IsGrounded(goal, known) && PlanningGoal.IsGrounded(goal, proposal)
            ? (goal, true)
            : (PlanningGoal.Fallback(proposal), false);
    }

    private static string? Goal(string? reply)
    {
        if (reply is null)
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(reply);
            return doc.RootElement.TryGetProperty("goal", out var g) && g.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(g.GetString())
                ? g.GetString()!.Trim()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
