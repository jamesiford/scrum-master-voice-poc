using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Features.Schedules.RunScheduleTick;

namespace Mcm.Scrum.Api.Scheduling;

/// <summary>Sends <see cref="RunScheduleTickCommand"/> every 15 seconds. All rules are in the command and the Domain evaluator.</summary>
public sealed class ScheduleWorker(IServiceScopeFactory scopes, ILogger<ScheduleWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ISender>().Send(new RunScheduleTickCommand(), stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Schedule tick failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}