namespace Mcm.Scrum.Domain.Kpis;

/// <summary>Sprint burndown figures.</summary>
/// <param name="CommittedPoints">All points in the sprint.</param>
/// <param name="CompletedPoints">Points on done items.</param>
/// <param name="RemainingPoints">Points still open.</param>
/// <param name="DaysLeft">Whole days left in the sprint, never negative.</param>
/// <param name="AtRisk">True when the burn needed exceeds 1.3 times the pace so far (SPEC 9.3).</param>
public sealed record Burndown(double CommittedPoints, double CompletedPoints, double RemainingPoints, int DaysLeft, bool AtRisk);
