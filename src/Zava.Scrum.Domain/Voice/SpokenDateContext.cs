using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Voice;

/// <summary>Code-owned team-local calendar facts, never supplied by a model.</summary>
/// <param name="Today">The date at the time the utterance is analysed, in the team's time zone.</param>
/// <param name="CurrentIteration">The prepared current iteration, when its dates are known.</param>
/// <param name="NextIteration">A verified subsequent iteration, never inferred from an assumed sprint length.</param>
public sealed record SpokenDateContext(DateOnly Today, IterationInfo? CurrentIteration = null, IterationInfo? NextIteration = null);
