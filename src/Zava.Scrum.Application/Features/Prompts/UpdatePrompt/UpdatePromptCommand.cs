using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Prompts.UpdatePrompt;

/// <summary>Replaces a prompt. Takes effect on the next turn, with no rebuild (rule 5).</summary>
/// <param name="TeamId">The team whose override is saved.</param>
/// <param name="Name">Prompt name.</param>
/// <param name="Text">New text.</param>
public sealed record UpdatePromptCommand(string TeamId, string Name, string Text) : ICommand;
