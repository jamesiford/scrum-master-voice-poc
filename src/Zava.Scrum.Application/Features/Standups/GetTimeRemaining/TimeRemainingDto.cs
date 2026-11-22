namespace Mcm.Scrum.Application.Features.Standups.GetTimeRemaining;

/// <summary>Time left.</summary>
/// <param name="RemainingSeconds">Seconds left, never negative.</param>
/// <param name="TotalSeconds">Timebox length.</param>
/// <param name="Warn">True once 80 percent has passed.</param>
public sealed record TimeRemainingDto(int RemainingSeconds, int TotalSeconds, bool Warn);
