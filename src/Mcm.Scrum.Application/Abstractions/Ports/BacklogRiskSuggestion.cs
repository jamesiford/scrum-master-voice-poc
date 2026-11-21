namespace Mcm.Scrum.Application.Abstractions.Ports;

/// <summary>Model-written phrasing for a pre-brief. Facts are never part of it.</summary>
/// <param name="Summary">Two-sentence team summary.</param>
/// <param name="QuestionsByMemberId">One suggested question per member id.</param>
public sealed record BacklogRiskSuggestion(string Summary, IReadOnlyDictionary<string, string> QuestionsByMemberId);
