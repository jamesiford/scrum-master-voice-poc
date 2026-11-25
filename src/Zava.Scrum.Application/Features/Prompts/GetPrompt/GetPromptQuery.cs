using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;

namespace Zava.Scrum.Application.Features.Prompts.GetPrompt;

/// <summary>Reads a prompt's text.</summary>
/// <param name="TeamId">The team whose override is looked for first.</param>
/// <param name="Name">Prompt name.</param>
public sealed record GetPromptQuery(string TeamId, string Name) : IQuery<PromptResolution>;
