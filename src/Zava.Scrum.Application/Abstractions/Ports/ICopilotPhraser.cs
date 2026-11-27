namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Phrases a brief introduction; figures, item citations and links remain code-owned.</summary>
public interface ICopilotPhraser
{
    /// <summary>Phrases the introduction using only the authorised, computed summary.</summary>
    /// <param name="teamId">The authorised team's identifier for source and trace isolation.</param>
    /// <param name="summary">Code-computed summary, without work item content or credentials.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>One introductory sentence, or null on model failure.</returns>
    Task<string?> PhraseAsync(string teamId, string summary, CancellationToken ct);
}
