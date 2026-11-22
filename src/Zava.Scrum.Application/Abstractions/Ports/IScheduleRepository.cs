using Mcm.Scrum.Domain.Schedules;

namespace Mcm.Scrum.Application.Abstractions.Ports;

/// <summary>Persistence for <see cref="Schedule"/>.</summary>
public interface IScheduleRepository
{
    /// <summary>Gets a team's schedule.</summary>
    /// <param name="teamId">Team id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The schedule, or <see langword="null"/> when none was saved.</returns>
    Task<Schedule?> GetAsync(string teamId, CancellationToken ct);

    /// <summary>Lists every saved schedule.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The schedules.</returns>
    Task<IReadOnlyList<Schedule>> ListAsync(CancellationToken ct);

    /// <summary>Inserts or replaces a schedule.</summary>
    /// <param name="schedule">The schedule.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task SaveAsync(Schedule schedule, CancellationToken ct);
}
