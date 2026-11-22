using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Calendars;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.Calendars.RejectSprintCalendar;

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
