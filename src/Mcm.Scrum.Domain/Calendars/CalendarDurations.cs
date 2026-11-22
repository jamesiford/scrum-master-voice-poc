namespace Mcm.Scrum.Domain.Calendars;

/// <summary>Meeting lengths and whether the team holds a sprint review.</summary>
/// <param name="PlanningMinutes">Sprint planning length.</param>
/// <param name="StandupMinutes">Daily standup length.</param>
/// <param name="ReviewMinutes">Sprint review length.</param>
/// <param name="RetroMinutes">Sprint retrospective length.</param>
/// <param name="HasSprintReview">Whether to schedule a sprint review (<c>Team:HasSprintReview</c>; Rajeev has not confirmed one).</param>
public sealed record CalendarDurations(int PlanningMinutes, int StandupMinutes, int ReviewMinutes, int RetroMinutes, bool HasSprintReview);
