using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Prompts.ListPrompts;

/// <summary>Lists prompt names.</summary>
public sealed record ListPromptsQuery : IQuery<IReadOnlyList<string>>;
