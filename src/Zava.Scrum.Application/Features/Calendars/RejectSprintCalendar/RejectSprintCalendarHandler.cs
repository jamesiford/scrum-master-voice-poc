using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Identity;
using Zava.Scrum.Domain.Calendars;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.Calendars.RejectSprintCalendar;

/// <summary>Rejects the calendar and replaces its card with the outcome.</summary>
internal sealed class RejectSprintCalendarHandler(ISprintCalendarRepository calendars, ApprovalGate gate, ITeamsMessenger messenger, IClock clock) : IRequestHandler<RejectSprintCalendarCommand, Result>
{
    public async Task<Result> Handle(RejectSprintCalendarCommand request, CancellationToken ct)
    {
        var calendar = await calendars.GetAsync(request.CalendarId, ct);
        if (calendar is null)
        {
            return Result.Failure(SprintCalendarErrors.NotFound(request.CalendarId));
        }

        var allowed = await gate.AuthoriseAsync(request.Decision, calendar.Data.TeamId, null, forScrumMaster: true, ct);
        if (!allowed.IsSuccess)
        {
            return allowed;
        }

        var rejected = calendar.Reject(request.Decision.Actor.DisplayName, clock.UtcNow);
        if (!rejected.IsSuccess)
        {
            return rejected;
        }

        await calendars.SaveAsync(calendar, ct);
        await messenger.UpdateAsync(CalendarCards.Result(calendar.Data, $"Rejected by {calendar.Data.DecidedBy}. Nothing was created."), ct);
        return Result.Success();
    }
}
