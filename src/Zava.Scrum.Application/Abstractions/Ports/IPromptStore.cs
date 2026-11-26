namespace Zava.Scrum.Application.Abstractions.Ports;

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

    /// <summary>
    /// Resolves a prompt for a team: the team's override, then the deployment-wide override, then the default file (ADR 0018).
    /// The default implementation ignores the team, so a store with no per-team overrides behaves as before.
    /// </summary>
    /// <param name="teamId">The team.</param>
    /// <param name="name">Prompt name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The text and where it came from, or <see langword="null"/> when the prompt does not exist.</returns>
    async Task<PromptResolution?> ResolveAsync(string teamId, string name, CancellationToken ct)
    {
        var text = await GetAsync(name, ct);
        return text is null ? null : new PromptResolution(text, PromptSource.Default);
    }

    /// <summary>Saves a team's override of a prompt. The default implementation saves it deployment-wide.</summary>
    /// <param name="teamId">The team.</param>
    /// <param name="name">Prompt name.</param>
    /// <param name="text">The new text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="false"/> when the prompt does not exist.</returns>
    Task<bool> SaveForTeamAsync(string teamId, string name, string text, CancellationToken ct) => SaveAsync(name, text, ct);
}

/// <summary>Where a resolved prompt came from.</summary>
public enum PromptSource
{
    /// <summary>The file in <c>agents/instructions</c>.</summary>
    Default,

    /// <summary>A deployment-wide override saved from the portal.</summary>
    DeploymentOverride,

    /// <summary>This team's override.</summary>
    TeamOverride,
}

/// <summary>A prompt and which level of the resolution order supplied it.</summary>
/// <param name="Text">The prompt text.</param>
/// <param name="Source">Where it came from.</param>
public sealed record PromptResolution(string Text, PromptSource Source);
