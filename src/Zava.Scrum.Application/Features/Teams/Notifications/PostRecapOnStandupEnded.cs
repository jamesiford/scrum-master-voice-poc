using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Features.Teams.Notifications;

/// <summary>
/// Posts the recap card when a standup ends. Facts come from recorded events, not from a model (rule 3). It is routed to the team's
/// recap channel and the meeting chat; only without either does it fall back to every stored conversation (M9-R5).
/// </summary>
internal sealed class PostRecapOnStandupEnded(ISessionRepository sessions, IMemberRepository members, ITeamRepository teams, ITeamsMessenger messenger) : INotificationHandler<StandupEnded>
{
    public async Task Handle(StandupEnded domainEvent, CancellationToken ct)
    {
        var session = await sessions.GetAsync(domainEvent.SessionId, ct);
        if (session is null)
        {
            return;
        }

        var roster = (await members.ListAsync(session.TeamId, ct)).ToDictionary(m => m.Id, m => m.DisplayName);
        var facts = session.ActiveEvents
            .Where(e => e.Type is StandupEventType.Blocker or StandupEventType.Commitment or StandupEventType.ParkingLot)
            .Select(e => new CardFact(
                $"{e.Type}{(e.MemberId is not null && roster.TryGetValue(e.MemberId, out var n) ? $" ({n})" : string.Empty)}",
                e.Text))
            .ToList();
        var team = await teams.GetAsync(session.TeamId, ct);
        var absent = session.Attendance.Count(a => a.Value == Attendance.Absent);
        var text = $"{facts.Count} items captured. {absent} absent.";
        await messenger.SendAsync(new TeamsMessage(CardKind.Recap, session.TeamId, session.Id, $"recap:{session.Id}", "Standup recap", text, facts, Route: CardRoute.Recap, RecapChannelKey: team?.RecapChannelKey), ct);
    }
}
