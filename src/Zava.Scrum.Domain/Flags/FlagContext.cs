using Mcm.Scrum.Domain.Teams;

namespace Mcm.Scrum.Domain.Flags;

/// <summary>Inputs shared by every <see cref="IFlagRule"/>. Time is passed in so rules stay pure.</summary>
/// <param name="Now">The current time.</param>
/// <param name="Behaviours">Team thresholds.</param>
public sealed record FlagContext(DateTimeOffset Now, TeamBehaviours Behaviours);
