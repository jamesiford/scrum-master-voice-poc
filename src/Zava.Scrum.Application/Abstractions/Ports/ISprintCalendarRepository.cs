using Zava.Scrum.Domain.Calendars;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Persistence for <see cref="SprintCalendar"/>. Every implementation must pass the shared contract test (SOLID: L).</summary>
public interface ISprintCalendarRepository
{
    /// <summary>Gets a calendar by id.</summary>
    /// <param name="id">Calendar id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The calendar, or <see langword="null"/>.</returns>
    Task<SprintCalendar?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>Lists the team's calendars, newest first.</summary>
    /// <param name="teamId">Team id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The calendars.</returns>
    Task<IReadOnlyList<SprintCalendar>> ListAsync(string teamId, CancellationToken ct);

    /// <summary>Inserts or replaces a calendar.</summary>
    /// <param name="calendar">The calendar.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task SaveAsync(SprintCalendar calendar, CancellationToken ct);
}
