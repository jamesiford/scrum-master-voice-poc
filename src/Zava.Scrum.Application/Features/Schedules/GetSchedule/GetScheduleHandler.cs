using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Schedules;

namespace Zava.Scrum.Application.Features.Schedules.GetSchedule;

/// <summary>Reads the schedule.</summary>
internal sealed class GetScheduleHandler(IScheduleRepository schedules) : IRequestHandler<GetScheduleQuery, Result<Schedule>>
{
    public async Task<Result<Schedule>> Handle(GetScheduleQuery request, CancellationToken ct) =>
        Result<Schedule>.Success(await schedules.GetAsync(request.TeamId, ct) ?? Schedule.Default(request.TeamId));
}
