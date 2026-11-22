using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Schedules.UpdateSchedule;

/// <summary>Saves a team's schedule.</summary>
/// <param name="TeamId">Team id.</param>
/// <param name="Enabled">Whether the scheduler acts on it.</param>
/// <param name="Days">Days of the week, for example <c>Monday</c>.</param>
/// <param name="StartTime">Local start time, <c>HH:mm</c>.</param>
/// <param name="Mode"><c>simulated</c> or <c>acs</c>.</param>
/// <param name="JoinUrl">Teams meeting link for <c>acs</c> mode.</param>
public sealed record UpdateScheduleCommand(string TeamId, bool Enabled, IReadOnlyList<string> Days, string StartTime, string Mode, string? JoinUrl) : ICommand;
