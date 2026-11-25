using System.ComponentModel.DataAnnotations;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Capacity;

namespace Zava.Scrum.Infrastructure.Teams;

/// <summary>Binds <c>Capacity__*</c>: how many standups this host may run at once. Bound the realtime model and media bot capacity here too.</summary>
public sealed class CapacityOptions : ICapacitySettings
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Capacity";

    /// <summary>Gets or sets how many standups may run at once on this host.</summary>
    [Range(1, 50)]
    public int MaxConcurrentStandups { get; set; } = 3;

    /// <summary>Gets or sets the minimum seconds between two starts.</summary>
    [Range(0, 300)]
    public int StaggerSeconds { get; set; } = 5;

    /// <summary>Gets or sets how many minutes a due standup may wait before the team is told it cannot start.</summary>
    [Range(1, 30)]
    public int GraceMinutes { get; set; } = 10;

    /// <inheritdoc />
    public CapacityLimits Limits => new(MaxConcurrentStandups, StaggerSeconds, GraceMinutes);
}
