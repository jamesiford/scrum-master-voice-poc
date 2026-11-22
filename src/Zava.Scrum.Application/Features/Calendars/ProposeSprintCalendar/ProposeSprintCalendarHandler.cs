using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Calendars;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.Calendars.ProposeSprintCalendar;

/// <summary>Plans the calendar, stores it and raises <see cref="SprintCalendarProposed"/>; a handler sends the card.</summary>
internal sealed class ProposeSprintCalendarHandler(SprintCalendarBuilder builder, ISprintCalendarRepository calendars, IPublisher publisher, IClock clock)
    : IRequestHandler<ProposeSprintCalendarCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ProposeSprintCalendarCommand request, CancellationToken ct)
    {
        var plan = await builder.BuildAsync(request.TeamId, request.Iteration, ct);
        if (!plan.IsSuccess)
        {
            return Result<Guid>.Failure(plan.Error!);
        }

        var calendar = SprintCalendar.Propose(Guid.NewGuid(), request.TeamId, plan.Value.Iteration.Path, plan.Value.Team.TimeZone, plan.Value.Meetings, clock.UtcNow);
        await calendars.SaveAsync(calendar, ct);
        await calendar.PublishEventsAsync(publisher, ct);
        return Result<Guid>.Success(calendar.Id);
    }
}
