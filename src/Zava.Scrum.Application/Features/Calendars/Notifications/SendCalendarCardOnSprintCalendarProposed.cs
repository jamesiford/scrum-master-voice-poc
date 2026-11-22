using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Calendars;

namespace Zava.Scrum.Application.Features.Calendars.Notifications;

/// <summary>Sends the "Proposed sprint calendar" card to the Scrum Master (outbox and Teams).</summary>
internal sealed class SendCalendarCardOnSprintCalendarProposed(ISprintCalendarRepository calendars, ITeamsMessenger messenger) : INotificationHandler<SprintCalendarProposed>
{
    public async Task Handle(SprintCalendarProposed domainEvent, CancellationToken ct)
    {
        var calendar = await calendars.GetAsync(domainEvent.CalendarId, ct);
        if (calendar is not null)
        {
            await messenger.SendAsync(CalendarCards.Initial(calendar.Data), ct);
        }
    }
}
