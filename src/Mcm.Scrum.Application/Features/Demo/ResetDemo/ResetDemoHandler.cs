using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
namespace Mcm.Scrum.Application.Features.Demo.ResetDemo;

/// <summary>Resets demo state, and clears today's run marks so a scheduled standup can fire again.</summary>
internal sealed class ResetDemoHandler(IDemoReset reset, IScheduleRepository schedules) : IRequestHandler<ResetDemoCommand, Result>
{
    public async Task<Result> Handle(ResetDemoCommand request, CancellationToken ct)
    {
        await reset.ResetAsync(ct);
        foreach (var s in await schedules.ListAsync(ct))
        {
            await schedules.SaveAsync(s with { PreparedFor = null, PreparedSessionId = null, StartedFor = null }, ct);
        }

        return Result.Success();
    }
}