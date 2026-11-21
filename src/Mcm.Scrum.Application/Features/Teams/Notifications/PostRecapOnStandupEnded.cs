using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Features.Teams.Notifications;

/// <summary>Posts the recap card when a standup ends. Facts come from recorded events, not from a model (rule 3).</summary>
internal sealed class PostRecapOnStandupEnded(ISessionRepository sessions, IMemberRepository members, ITeamsMessenger messenger) : INotificationHandler<StandupEnded>
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
        var absent = session.Attendance.Count(a => a.Value == Attendance.Absent);
        var text = $"{facts.Count} items captured. {absent} absent.";
        await messenger.SendAsync(new TeamsMessage(CardKind.Recap, session.TeamId, session.Id, $"recap:{session.Id}", "Standup recap", text, facts), ct);
    }
}
