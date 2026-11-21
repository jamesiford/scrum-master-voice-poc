namespace Mcm.Scrum.Application.Abstractions.Ports;

/// <summary>Source of the current time so Domain and handlers stay deterministic under test.</summary>
public interface IClock
{
    /// <summary>Gets the current UTC time.</summary>
    DateTimeOffset UtcNow { get; }
}
