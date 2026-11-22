namespace Mcm.Scrum.Application.Abstractions.Ports;

/// <summary>Reads agent instructions by name. Prompts live in <c>agents/instructions/*.md</c> so they can be tuned without a rebuild (rule 5).</summary>
public interface IPromptStore
{
    /// <summary>Gets the prompt text.</summary>
    /// <param name="name">File name without extension, for example <c>backlog-risk</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The text, or <see langword="null"/> when no such prompt exists.</returns>
    Task<string?> GetAsync(string name, CancellationToken ct);

    /// <summary>Lists the prompt names.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Names without extension.</returns>
    Task<IReadOnlyList<string>> ListAsync(CancellationToken ct);

    /// <summary>Replaces an existing prompt.</summary>
    /// <param name="name">Prompt name.</param>
    /// <param name="text">New text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true"/> when the prompt exists and was saved.</returns>
    Task<bool> SaveAsync(string name, string text, CancellationToken ct);
}
