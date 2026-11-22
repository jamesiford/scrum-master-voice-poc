using System.Text.Json;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Standups;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Application.Features.Retrospectives.NameRetroThemes;

/// <summary>Stores one <see cref="StandupEventType.Theme"/> event per group, in the same order <see cref="ThemeGrouper"/> returns them.</summary>
internal sealed class NameRetroThemesHandler(ISessionRepository sessions, IDraftingAgent agent, IClock clock) : IRequestHandler<NameRetroThemesCommand, Result>
{
    private const int MaxName = 40;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<Result> Handle(NameRetroThemesCommand request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        if (session.ActiveEvents.Any(e => e.Type == StandupEventType.Theme))
        {
            return Result.Success();
        }

        var notes = session.ActiveEvents.Where(e => e.Type == StandupEventType.RetroItem && e.Category is not null).ToList();
        var themes = ThemeGrouper.Group(notes);
        if (themes.Count == 0)
        {
            return Result.Success();
        }

        var names = await NamesAsync(themes, notes, SprintIds.From(session.PreBrief), ct);
        foreach (var name in names)
        {
            session.RecordEvent(StandupEventType.Theme, null, [], name, clock.UtcNow);
        }

        await sessions.SaveAsync(session, ct);
        return Result.Success();
    }

    // The model sees only the grouped notes and may only return names; a reply with the wrong count, or a name that cites an item, is dropped.
    private async Task<IReadOnlyList<string>> NamesAsync(IReadOnlyList<RetroTheme> themes, List<StandupEvent> notes, IReadOnlySet<WorkItemId> known, CancellationToken ct)
    {
        var fallback = themes.Select(t => char.ToUpperInvariant(t.Keyword[0]) + t.Keyword[1..]).ToList();
        var facts = new { themes = themes.Select(t => new { t.Keyword, notes = notes.Where(n => t.ItemIds.Contains(n.Id)).Select(n => n.Text) }) };
        var named = Parse(await agent.DraftJsonAsync("retro-themes", JsonSerializer.Serialize(facts, Json), ct));
        return named is not null && named.Count == themes.Count && named.All(n => WorkItemIdGuard.IsGrounded(n, known)) ? named : fallback;
    }

    private static List<string>? Parse(string? reply)
    {
        if (reply is null)
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(reply);
            if (!doc.RootElement.TryGetProperty("names", out var arr) || arr.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var names = arr.EnumerateArray().Select(e => e.ValueKind == JsonValueKind.String ? e.GetString()?.Trim() : null).ToList();
            return names.All(n => !string.IsNullOrWhiteSpace(n) && n.Length <= MaxName) ? names.Select(n => n!).ToList() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
