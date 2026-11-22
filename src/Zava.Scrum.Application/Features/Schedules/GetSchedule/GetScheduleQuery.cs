using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.Schedules;

namespace Zava.Scrum.Application.Features.Schedules.GetSchedule;

/// <summary>Reads a team's schedule; the default (off) when none was saved.</summary>
/// <param name="TeamId">Team id.</param>
public sealed record GetScheduleQuery(string TeamId) : IQuery<Schedule>;
