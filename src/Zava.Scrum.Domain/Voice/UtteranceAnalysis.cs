namespace Zava.Scrum.Domain.Voice;

/// <summary>The grounded reading of one utterance: what it is, which items it is about, and what was said about them.</summary>
/// <param name="Interpretation">Step 1: the intent with its target and proxy members.</param>
/// <param name="WorkItemIds">Step 2: sprint items the utterance is about. Every id exists in the sprint (rule 2).</param>
/// <param name="Comments">Step 3: classified statements about those items; empty from the rules.</param>
/// <param name="Source">Whether the model or the rules produced it.</param>
/// <param name="ModelMs">How long the decision model call took, in milliseconds; 0 when it was not asked. Diagnostic only.</param>
public sealed record UtteranceAnalysis(Interpretation Interpretation, IReadOnlyList<int> WorkItemIds, IReadOnlyList<ItemComment> Comments, AnalysisSource Source, int ModelMs = 0);
