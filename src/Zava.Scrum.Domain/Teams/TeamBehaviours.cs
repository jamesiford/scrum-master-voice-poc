namespace Zava.Scrum.Domain.Teams;

/// <summary>Tunable thresholds for flag rules and escalation.</summary>
/// <param name="StaleDays">An in-progress item older than this many days in its state is stale.</param>
/// <param name="EscalateBlockerAfterDays">A blocker older than this many days is escalated.</param>
/// <param name="AtRiskDays">An item due within this many days may be at risk.</param>
/// <param name="AtRiskRemainingPct">An item due soon is at risk when more than this share of its original estimate remains (0 to 1).</param>
public sealed record TeamBehaviours(int StaleDays = 2, int EscalateBlockerAfterDays = 2, int AtRiskDays = 2, double AtRiskRemainingPct = 0.5);
