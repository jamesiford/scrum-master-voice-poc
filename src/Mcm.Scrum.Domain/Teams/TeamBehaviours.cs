namespace Mcm.Scrum.Domain.Teams;

/// <summary>Tunable thresholds for flag rules and escalation.</summary>
/// <param name="StaleDays">An in-progress item older than this many days in its state is stale.</param>
/// <param name="EscalateBlockerAfterDays">A blocker older than this many days is escalated.</param>
public sealed record TeamBehaviours(int StaleDays = 2, int EscalateBlockerAfterDays = 2);
