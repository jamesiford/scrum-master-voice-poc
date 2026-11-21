using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Domain.Schedules;

namespace Mcm.Scrum.Application.Features.Schedules.GetSchedule;

/// <summary>Reads a team's schedule; the default (off) when none was saved.</summary>
/// <param name="TeamId">Team id.</param>
public sealed record GetScheduleQuery(string TeamId) : IQuery<Schedule>;
