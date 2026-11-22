using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Schedules.RunScheduleTick;

/// <summary>Runs whatever is due on every team's schedule. Sent every few seconds by the scheduler; safe to repeat. Returns how many actions ran.</summary>
public sealed record RunScheduleTickCommand : ICommand<int>;
