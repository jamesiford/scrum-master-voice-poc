using Microsoft.Agents.AI;

namespace Zava.Scrum.Infrastructure.Agents;

/// <summary>
/// Creates the Microsoft Agent Framework agent that runs one prompt. A seam so the production factory talks to Foundry while tests
/// supply a fake chat client behind a real <see cref="ChatClientAgent"/>.
/// </summary>
public interface IFoundryAgentFactory
{
    /// <summary>Creates an agent for one call.</summary>
    /// <param name="name">Agent name, used in traces.</param>
    /// <param name="instructions">System instructions (the prompt text from <c>agents/instructions</c>).</param>
    /// <returns>The agent.</returns>
    AIAgent Create(string name, string instructions);

    /// <summary>Creates an agent on a specific model deployment, for a call that needs a faster or cheaper model than the default.</summary>
    /// <param name="name">Agent name, used in traces.</param>
    /// <param name="instructions">System instructions.</param>
    /// <param name="model">The deployment name, or <see langword="null"/> for the default reasoning deployment.</param>
    /// <returns>The agent.</returns>
    AIAgent Create(string name, string instructions, string? model) => Create(name, instructions);
}
