using Mcm.Scrum.Application.Abstractions.Ports;

namespace Mcm.Scrum.Infrastructure.Agents;

/// <summary>Runs a named prompt against the Foundry chat deployment and returns its JSON reply.</summary>
public sealed class FoundryDraftingAgent(FoundryChat chat, IPromptStore prompts) : IDraftingAgent
{
    /// <inheritdoc />
    public async Task<string?> DraftJsonAsync(string promptName, string inputJson, CancellationToken ct)
    {
        if (!chat.IsConfigured)
        {
            return null;
        }

        var instructions = await prompts.GetAsync(promptName, ct);
        return instructions is null ? null : await chat.TryCompleteJsonAsync(instructions, inputJson, ct);
    }
}
