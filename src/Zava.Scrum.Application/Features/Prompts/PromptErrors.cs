using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.Prompts;

/// <summary>Error codes for prompt use cases.</summary>
public static class PromptErrors
{
    /// <summary>Gets the error for an unknown prompt.</summary>
    /// <param name="name">The prompt name.</param>
    /// <returns>The error.</returns>
    public static Error NotFound(string name) => Error.NotFound("prompt.not_found", $"Prompt '{name}' does not exist.");
}
