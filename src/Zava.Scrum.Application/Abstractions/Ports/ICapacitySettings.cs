using Zava.Scrum.Domain.Capacity;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>The capacity limits for this host: concurrent standups, the stagger between starts and the grace window (ADR 0018).</summary>
public interface ICapacitySettings
{
    /// <summary>Gets the limits.</summary>
    CapacityLimits Limits { get; }
}
