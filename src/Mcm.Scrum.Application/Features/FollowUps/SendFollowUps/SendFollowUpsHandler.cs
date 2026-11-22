using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Application.Features.Standups;
using Mcm.Scrum.Application.Features.Teams;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.FollowUps;
using Mcm.Scrum.Domain.Members;
using Mcm.Scrum.Domain.Standups;

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
    IEmailSender emails,
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
            await messenger.SendAsync(FollowUpCards.Initial(followUp, NameOf(followUp, roster)), ct);
            await EmailAsync(followUp, roster, ct);
        }

        return Result<int>.Success(planned.Count);
    }

    // Email is an extra channel for the same follow-up (F3 stretch); the Teams card stays the approval surface.
    private async Task EmailAsync(FollowUp f, Dictionary<string, Member> roster, CancellationToken ct)
    {
        if (!emails.Enabled || f.MemberId is null || !roster.TryGetValue(f.MemberId, out var m) || string.IsNullOrWhiteSpace(m.AdoUniqueName))
        {
            return;
        }

        await emails.SendAsync(new EmailMessage(m.AdoUniqueName, Subject(f, m), f.Message, f.Id), ct);
    }

    private static string Subject(FollowUp f, Member m) => f.Kind == FollowUpKind.Unblock
        ? $"Can you help unblock a teammate, {m.DisplayName}?"
        : $"{char.ToUpperInvariant(f.Ceremony.Phrase()[0])}{f.Ceremony.Phrase()[1..]} follow-up for {m.DisplayName}";

    private static string NameOf(FollowUp f, Dictionary<string, Member> roster) =>
        f.MemberId is not null && roster.TryGetValue(f.MemberId, out var m) ? m.DisplayName : "the team";
}
