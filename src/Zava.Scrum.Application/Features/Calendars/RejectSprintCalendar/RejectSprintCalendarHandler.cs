using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Calendars;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.Calendars.RejectSprintCalendar;

/// <summary>Rejects the calendar and replaces its card with the outcome.</summary>
internal sealed class RejectSprintCalendarHandler(ISprintCalendarRepository calendars, ITeamsMessenger messenger, IClock clock) : IRequestHandler<RejectSprintCalendarCommand, Result>
{
    public async Task<Result> Handle(RejectSprintCalendarCommand request, CancellationToken ct)
    {
        var calendar = await calendars.GetAsync(request.CalendarId, ct);
        if (calendar is null)
        {
            return Result.Failure(SprintCalendarErrors.NotFound(request.CalendarId));
        }

        var rejected = calendar.Reject(request.Rejecter, clock.UtcNow);
        if (!rejected.IsSuccess)
        {
            return rejected;
        }

        await calendars.SaveAsync(calendar, ct);
        await messenger.UpdateAsync(CalendarCards.Result(calendar.Data, $"Rejected by {calendar.Data.DecidedBy}. Nothing was created."), ct);
        return Result.Success();
    }
}
