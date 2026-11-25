using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Identity;
using Zava.Scrum.Domain.Calendars;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Schedules;

namespace Zava.Scrum.Application.Features.Calendars.ApproveSprintCalendar;

/// <summary>
/// Creates only the meetings <see cref="SprintCalendar.Approve"/> returns, so nothing reaches anyone's calendar without a named approver. A failure
/// part-way keeps what was created (the calendar stays approving) and a retry creates the rest; each meeting has a stable transaction id.
/// </summary>
internal sealed class ApproveSprintCalendarHandler(
    ISprintCalendarRepository calendars,
    ICalendarWriter writer,
    IMemberRepository members,
    IScheduleRepository schedules,
    ApprovalGate gate,
    ITeamsMessenger messenger,
    IClock clock) : IRequestHandler<ApproveSprintCalendarCommand, Result>
{
    public async Task<Result> Handle(ApproveSprintCalendarCommand request, CancellationToken ct)
    {
        var calendar = await calendars.GetAsync(request.CalendarId, ct);
        if (calendar is null)
        {
            return Result.Failure(SprintCalendarErrors.NotFound(request.CalendarId));
        }

        if (!writer.Enabled)
        {
            return Result.Failure(SprintCalendarErrors.NotConfigured);
        }

        var allowed = await gate.AuthoriseAsync(request.Decision, calendar.Data.TeamId, null, forScrumMaster: true, ct);
        if (!allowed.IsSuccess)
        {
            return allowed;
        }

        var approved = calendar.Approve(request.Decision.Actor.DisplayName, clock.UtcNow);
        if (!approved.IsSuccess)
        {
            return Result.Failure(approved.Error!);
        }

        var attendees = (await members.ListAsync(calendar.Data.TeamId, ct)).Where(m => m.Active && m.AdoUniqueName?.Contains('@', StringComparison.Ordinal) == true).Select(m => m.AdoUniqueName!).ToList();
        Error? failure = null;
        foreach (var meeting in approved.Value)
        {
            var created = await writer.CreateMeetingAsync(new MeetingInvite($"{calendar.Id}:{meeting.Key}", meeting.Title, meeting.StartsAt, meeting.EndsAt, attendees), ct);
            if (!created.IsSuccess)
            {
                failure = created.Error;
                calendar.RecordFailure(created.Error!.Message);
                break;
            }

            calendar.RecordCreated(meeting.Key, created.Value.EventId, created.Value.JoinUrl);
        }

        calendar.Complete();
        await calendars.SaveAsync(calendar, ct);
        await StoreJoinUrlsAsync(calendar.Data, ct);
        var status = failure is null ? $"Approved by {calendar.Data.DecidedBy}. {calendar.Data.Meetings.Count} meetings are in the calendar." : $"Stopped: {failure.Message} Approve again to create the rest.";
        await messenger.UpdateAsync(CalendarCards.Result(calendar.Data, status), ct);
        return failure is null ? Result.Success() : Result.Failure(failure);
    }

    // The scheduler joins the standup of the day with the calendar's meeting URL. Approving also turns the scheduler on in acs mode: that is
    // what "the next standup auto-joins" needs. Later approvals replace the dates they cover.
    private async Task StoreJoinUrlsAsync(SprintCalendarData data, CancellationToken ct)
    {
        var zone = TimeZoneInfo.TryFindSystemTimeZoneById(data.TimeZone, out var z) ? z : TimeZoneInfo.Utc;
        var fresh = data.Meetings
            .Where(m => m.Kind == MeetingKind.Standup && m.JoinUrl is not null)
            .Select(m => new ScheduledMeeting(m.LocalDate(zone), m.JoinUrl!))
            .ToList();
        if (fresh.Count == 0)
        {
            return;
        }

        var schedule = await schedules.GetAsync(data.TeamId, ct) ?? Schedule.Default(data.TeamId);
        var kept = (schedule.Meetings ?? []).Where(m => fresh.All(f => f.Date != m.Date));
        await schedules.SaveAsync(schedule with { Enabled = true, Mode = "acs", Meetings = [.. kept, .. fresh] }, ct);
    }
}
