using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Calendars;

namespace Mcm.Scrum.Application.Features.Calendars.Notifications;

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
