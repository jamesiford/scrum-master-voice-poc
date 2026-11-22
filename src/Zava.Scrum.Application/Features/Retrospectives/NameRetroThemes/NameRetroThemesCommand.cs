using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Retrospectives.NameRetroThemes;

/// <summary>
/// Groups the retrospective's notes by shared keywords in code and asks a model only to name the groups (code fallback: the shared keyword).
/// Idempotent: a session that already has themes is left alone.
/// </summary>
/// <param name="SessionId">Session id.</param>
public sealed record NameRetroThemesCommand(Guid SessionId) : ICommand;
