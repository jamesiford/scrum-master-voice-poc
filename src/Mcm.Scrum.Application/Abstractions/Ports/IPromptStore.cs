namespace Mcm.Scrum.Application.Abstractions.Ports;

/// <summary>Reads agent instructions by name. Prompts live in <c>agents/instructions/*.md</c> so they can be tuned without a rebuild (rule 5).</summary>
public interface IPromptStore
{
    /// <summary>Gets the prompt text.</summary>
    /// <param name="name">File name without extension, for example <c>backlog-risk</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The text, or <see langword="null"/> when no such prompt exists.</returns>
    Task<string?> GetAsync(string name, CancellationToken ct);
}
