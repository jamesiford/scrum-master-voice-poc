using Mcm.Scrum.Domain.Voice;

namespace Mcm.Scrum.Domain.Standups;

/// <summary>
/// The retrospective's order: Opening, then a Round (each present member once, with the standup's step-away, silence and circle-back rules),
/// then Themes (the agent reads the grouped notes back), then Actions (up to <see cref="MaxActions"/>; present members are asked in speaking
/// order until there are enough), then Close (the agent's recap). The phase is derived from the session's events, so nothing new is persisted
/// on the aggregate.
/// </summary>
public sealed class RetroAgenda : ICeremonyAgenda
{
    /// <summary>The most notes one member can add in the Round.</summary>
    public const int MaxItemsPerMember = 3;

    /// <summary>The most action items a retrospective records.</summary>
    public const int MaxActions = 3;

    /// <summary>The text of the <see cref="StandupEventType.Phase"/> event that marks the Themes read-back.</summary>
    public const string ThemesMarker = "Themes";

    /// <summary>The prefix of the <see cref="StandupEventType.Phase"/> event recorded when a member is asked for an action.</summary>
    public const string ActionTurnPrefix = "ActionTurn:";

    /// <summary>Gets the shared instance; the agenda has no state.</summary>
    public static RetroAgenda Instance { get; } = new();

    /// <summary>Checks whether the retrospective has reached the Actions phase (the themes were read back).</summary>
    /// <param name="session">The session.</param>
    /// <returns><see langword="true"/> from the themes read-back onwards.</returns>
    public static bool InActions(StandupSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.Events.Any(e => e.Type == StandupEventType.Phase && e.Text == ThemesMarker);
    }

    /// <inheritdoc />
    public AgendaStep? Next(StandupSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var round = StandupAgenda.Instance.Next(session);
        if (round is not null)
        {
            return round;
        }

        if (!session.Events.Any(e => e.Type == StandupEventType.Phase && e.Text == ThemesMarker))
        {
            return new Announcement(SpeakReason.RetroThemes, ThemesMarker);
        }

        if (session.ActiveEvents.Count(e => e.Type == StandupEventType.ActionItem) >= MaxActions)
        {
            return null;
        }

        var asked = session.Events
            .Where(e => e.Type == StandupEventType.Phase && e.Text.StartsWith(ActionTurnPrefix, StringComparison.Ordinal))
            .Select(e => e.Text[ActionTurnPrefix.Length..])
            .ToHashSet();
        var next = session.PreBrief?.Members.FirstOrDefault(m => !asked.Contains(m.MemberId) && IsPresent(session, m.MemberId));
        return next is null ? null : new MemberTurn(next, false, false, "Actions");
    }

    // Someone who was absent, or who was in the meeting but never answered in the Round, is not asked for an action.
    private static bool IsPresent(StandupSession session, string memberId) =>
        !(session.Attendance.TryGetValue(memberId, out var a) && a == Attendance.Absent)
        && !(session.NoResponseCount(memberId) > 0 && !session.HasSpoken(memberId));
}
