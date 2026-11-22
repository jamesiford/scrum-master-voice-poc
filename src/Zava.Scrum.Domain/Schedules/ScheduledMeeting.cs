namespace Zava.Scrum.Domain.Schedules;

/// <summary>A standup meeting created in the agent's calendar.</summary>
/// <param name="Date">The standup date (team-local).</param>
/// <param name="JoinUrl">The Teams join URL.</param>
public sealed record ScheduledMeeting(DateOnly Date, string JoinUrl);
