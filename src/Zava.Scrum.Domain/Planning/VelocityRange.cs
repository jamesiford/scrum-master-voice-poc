namespace Zava.Scrum.Domain.Planning;

/// <summary>The range of points the team completed in recent sprints.</summary>
/// <param name="Low">Lowest completed points.</param>
/// <param name="High">Highest completed points.</param>
public sealed record VelocityRange(double Low, double High);
