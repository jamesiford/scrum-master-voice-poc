namespace Mcm.Scrum.Application.Abstractions.Ports;

/// <summary>Drafts text with a model from a named prompt. Draft only: nothing it returns is written anywhere without approval.</summary>
public interface IDraftingAgent
{
    /// <summary>Runs a prompt and returns the model's JSON reply.</summary>
    /// <param name="promptName">Prompt name under <c>agents/instructions</c>.</param>
    /// <param name="inputJson">The JSON facts or request sent as the user message.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The JSON reply, or <see langword="null"/> when no model is configured or the call failed.</returns>
    Task<string?> DraftJsonAsync(string promptName, string inputJson, CancellationToken ct);
}
