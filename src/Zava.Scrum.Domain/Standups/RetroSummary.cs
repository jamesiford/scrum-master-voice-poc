namespace Zava.Scrum.Domain.Standups;

/// <summary>One note on the retrospective board.</summary>
/// <param name="Id">The event id.</param>
/// <param name="Text">The note.</param>
/// <param name="MemberId">Who raised it.</param>
/// <param name="Category">The column, or <see langword="null"/> when never sorted.</param>
public sealed record RetroNoteView(Guid Id, string Text, string? MemberId, RetroCategory? Category);

/// <summary>One agreed action.</summary>
/// <param name="Text">What to do.</param>
/// <param name="OwnerMemberId">The roster member who owns it.</param>
/// <param name="OwnerName">Their display name.</param>
public sealed record RetroActionView(string Text, string OwnerMemberId, string OwnerName);

/// <summary>One theme with its name and size.</summary>
/// <param name="Name">The name the model gave it, or the shared keyword when it has not been named.</param>
/// <param name="Count">How many notes are in it.</param>
public sealed record RetroThemeView(string Name, int Count);

/// <summary>Everything a retrospective produced, computed from the session's events in code.</summary>
/// <param name="Notes">The sorted and unsorted notes, in the order they were raised.</param>
/// <param name="Themes">Themes, largest first.</param>
/// <param name="Actions">The agreed actions.</param>
public sealed record RetroSummary(IReadOnlyList<RetroNoteView> Notes, IReadOnlyList<RetroThemeView> Themes, IReadOnlyList<RetroActionView> Actions)
{
    /// <summary>Builds the summary.</summary>
    /// <param name="session">The retrospective session.</param>
    /// <returns>The summary.</returns>
    public static RetroSummary Build(StandupSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var active = session.ActiveEvents.ToList();
        var items = active.Where(e => e.Type == StandupEventType.RetroItem).ToList();
        var notes = items.Select(e => new RetroNoteView(e.Id, e.Text, e.MemberId, e.Category)).ToList();
        var names = active.Where(e => e.Type == StandupEventType.Theme).Select(e => e.Text).ToList();
        var themes = ThemeGrouper.Group(items.Where(e => e.Category is not null).ToList())
            .Select((t, i) => new RetroThemeView(i < names.Count ? names[i] : Capitalise(t.Keyword), t.ItemIds.Count))
            .ToList();
        var roster = session.PreBrief?.Members ?? [];
        var actions = active
            .Where(e => e.Type == StandupEventType.ActionItem && e.OwnerMemberId is not null)
            .Select(e => new RetroActionView(e.Text, e.OwnerMemberId!, roster.FirstOrDefault(m => m.MemberId == e.OwnerMemberId)?.DisplayName ?? e.OwnerMemberId!))
            .ToList();
        return new RetroSummary(notes, themes, actions);
    }

    /// <summary>Gets the notes in one column.</summary>
    /// <param name="category">The column.</param>
    /// <returns>The notes.</returns>
    public IReadOnlyList<RetroNoteView> In(RetroCategory category) => Notes.Where(n => n.Category == category).ToList();

    private static string Capitalise(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
