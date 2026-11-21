using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Application.Features.Standups;
using Mcm.Scrum.Application.Features.Teams;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.FollowUps;
using Mcm.Scrum.Domain.Members;

namespace Mcm.Scrum.Application.Features.FollowUps.SendFollowUps;

/// <summary>
/// Plans follow-ups with the pure <see cref="FollowUpPlanner"/> and sends one card each. Escalation is a follow-up kind
/// here, not a separate command, because it uses the same planner and card path (ADR 0004).
/// </summary>
internal sealed class SendFollowUpsHandler(
    ISessionRepository sessions,
    ITeamRepository teams,
    IMemberRepository members,
    IFollowUpRepository followUps,
    ITeamsMessenger messenger,
    IClock clock) : IRequestHandler<SendFollowUpsCommand, Result<int>>
{
    public async Task<Result<int>> Handle(SendFollowUpsCommand request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result<int>.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        var existing = await followUps.ListBySessionAsync(session.Id, ct);
        if (existing.Count > 0)
        {
            return Result<int>.Success(existing.Count);
        }

        var team = await teams.GetAsync(session.TeamId, ct);
        if (team is null)
        {
            return Result<int>.Failure(TeamErrors.NotFound(session.TeamId));
        }

        var roster = (await members.ListAsync(team.Id, ct)).ToDictionary(m => m.Id);
        var planned = FollowUpPlanner.Plan(session, team.Behaviours.EscalateBlockerAfterDays, Guid.NewGuid, clock.UtcNow);
        foreach (var followUp in planned)
        {
            followUp.MarkSent();
            await followUps.SaveAsync(followUp, ct);
            await messenger.SendAsync(ToMessage(followUp, roster), ct);
        }

        return Result<int>.Success(planned.Count);
    }

    private static TeamsMessage ToMessage(FollowUp f, Dictionary<string, Member> roster)
    {
        var name = f.MemberId is not null && roster.TryGetValue(f.MemberId, out var m) ? m.DisplayName : "the team";
        var (kind, title) = f.Kind switch
        {
            FollowUpKind.Absent => (CardKind.Absent, $"Missed standup: {name}"),
            FollowUpKind.Escalation => (CardKind.Escalation, "Blocker needs your help"),
            _ => (CardKind.FollowUp, $"Follow-up for {name}"),
        };
        var facts = f.Changes.Select(c => new CardFact($"Work item {c.WorkItemId} ({c.Op})", c.Value)).ToList();
        return new TeamsMessage(kind, f.TeamId, f.SessionId, $"followup:{f.Id}", title, f.Message, facts, f.Id, f.MemberId);
    }
}
