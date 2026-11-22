using System.ComponentModel.DataAnnotations;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Binds <c>Flags__*</c>: the default flag and escalation thresholds a team starts with. The portal can change them per team.</summary>
public sealed class FlagsOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Flags";

    /// <summary>Gets or sets the days in state before an in-progress item is stale.</summary>
    [Range(0, 30)]
    public int StaleDays { get; set; } = 2;

    /// <summary>Gets or sets the days a blocker may last before it is escalated to the Scrum Master.</summary>
    [Range(0, 30)]
    public int EscalateBlockerAfterDays { get; set; } = 2;

    /// <summary>Gets or sets the window in days before the due date in which an item may be at risk.</summary>
    [Range(0, 30)]
    public int AtRiskDays { get; set; } = 2;

    /// <summary>Gets or sets the share of the original estimate still remaining (0 to 1) above which a due-soon item is at risk.</summary>
    [Range(0.0, 1.0)]
    public double AtRiskRemainingPct { get; set; } = 0.5;

    /// <summary>Converts the options to team thresholds.</summary>
    /// <returns>The thresholds.</returns>
    public TeamBehaviours ToBehaviours() => new(StaleDays, EscalateBlockerAfterDays, AtRiskDays, AtRiskRemainingPct);
}
