using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Calendars;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.Calendars.GetSprintCalendar;

/// <summary>Plans the calendar in Domain code and formats it.</summary>
internal sealed class GetSprintCalendarHandler(SprintCalendarBuilder builder, IClock clock) : IRequestHandler<GetSprintCalendarQuery, Result<SprintCalendarDto>>
{
    public async Task<Result<SprintCalendarDto>> Handle(GetSprintCalendarQuery request, CancellationToken ct)
    {
        var plan = await builder.BuildAsync(request.TeamId, request.Iteration, ct);
        return plan.IsSuccess
            ? Result<SprintCalendarDto>.Success(new SprintCalendarDto(plan.Value.Iteration.Path, plan.Value.Team.TimeZone, plan.Value.Meetings, IcsFormatter.Format(plan.Value.Iteration.Name, plan.Value.Meetings, clock.UtcNow)))
            : Result<SprintCalendarDto>.Failure(plan.Error!);
    }
}
