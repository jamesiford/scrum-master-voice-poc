using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Abstractions.Ports;

/// <summary>Read-only Backlog and Risk agent (SPEC 10.2): phrases questions and a summary for a pre-brief built in code.</summary>
public interface IBacklogRiskAgent
{
    /// <summary>Suggests questions and a summary.</summary>
    /// <param name="brief">The factual pre-brief.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Suggestions, or <see langword="null"/> when the model is unavailable (the caller keeps its deterministic text).</returns>
    Task<BacklogRiskSuggestion?> SuggestAsync(PreBrief brief, CancellationToken ct);
}
