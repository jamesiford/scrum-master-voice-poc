namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Phrases reports and answers questions. It receives facts computed in code and must not add numbers (rule 3).</summary>
public interface IReportingAgent
{
    /// <summary>Writes a short narrative over the given facts.</summary>
    /// <param name="kind"><c>sprint</c> or <c>monthly</c>.</param>
    /// <param name="factsJson">The facts as JSON.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The narrative, or <see langword="null"/> when the model is unavailable.</returns>
    Task<string?> NarrateAsync(string kind, string factsJson, CancellationToken ct);

    /// <summary>Answers a question using only the given facts.</summary>
    /// <param name="question">The question.</param>
    /// <param name="factsJson">The facts as JSON.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The answer, or <see langword="null"/> when the model is unavailable.</returns>
    Task<string?> AnswerAsync(string question, string factsJson, CancellationToken ct);
}
