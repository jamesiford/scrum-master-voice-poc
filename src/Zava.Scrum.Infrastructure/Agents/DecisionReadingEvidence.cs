using Zava.Scrum.Domain.Voice;

namespace Zava.Scrum.Infrastructure.Agents;

/// <summary>Content-free decision probabilities for offline calibration; never a substitute for grounding.</summary>
/// <param name="Reading">The same typed reading the live adapter returns.</param>
/// <param name="Probabilities">Reported probabilities, before the 0.7 and 0.6 thresholds; absent values stay absent.</param>
/// <param name="Failure">Explicit failure classification, or null for a complete model response.</param>
public sealed record DecisionReadingEvidence(UtteranceReading? Reading, IReadOnlyDictionary<string, double> Probabilities, string? Failure);
