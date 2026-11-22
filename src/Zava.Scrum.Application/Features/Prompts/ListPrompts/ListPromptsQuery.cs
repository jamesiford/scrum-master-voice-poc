using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Prompts.ListPrompts;

/// <summary>Lists prompt names.</summary>
public sealed record ListPromptsQuery : IQuery<IReadOnlyList<string>>;
