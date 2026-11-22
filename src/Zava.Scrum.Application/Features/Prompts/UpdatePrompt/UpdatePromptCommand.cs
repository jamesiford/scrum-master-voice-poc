using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Prompts.UpdatePrompt;

/// <summary>Replaces a prompt. Takes effect on the next turn, with no rebuild (rule 5).</summary>
/// <param name="Name">Prompt name.</param>
/// <param name="Text">New text.</param>
public sealed record UpdatePromptCommand(string Name, string Text) : ICommand;
