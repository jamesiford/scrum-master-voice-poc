using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Schedules;

namespace Mcm.Scrum.Application.Features.Schedules.GetSchedule;

/// <summary>Reads the schedule.</summary>
internal sealed class GetScheduleHandler(IScheduleRepository schedules) : IRequestHandler<GetScheduleQuery, Result<Schedule>>
{
    public async Task<Result<Schedule>> Handle(GetScheduleQuery request, CancellationToken ct) =>
        Result<Schedule>.Success(await schedules.GetAsync(request.TeamId, ct) ?? Schedule.Default(request.TeamId));
}
