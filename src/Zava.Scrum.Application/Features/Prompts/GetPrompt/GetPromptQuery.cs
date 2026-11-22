using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Prompts.GetPrompt;

/// <summary>Reads a prompt's text.</summary>
/// <param name="Name">Prompt name.</param>
public sealed record GetPromptQuery(string Name) : IQuery<string>;
