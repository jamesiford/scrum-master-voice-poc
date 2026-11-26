using Zava.Scrum.Application.Features.Standups;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Api.Voice;

/// <summary>Projects live native board facts using existing Domain rules, with no repository or publisher.</summary>
public sealed class EphemeralMediaBoard(SessionDto session, string joinUrl)
{
    private readonly HashSet<int> _covered = [];
    private readonly Dictionary<string, int> _focus = [];
    private readonly Dictionary<string, Attendance> _attendance = [];
    private readonly Dictionary<string, string?> _status = [];
    private readonly HashSet<string> _handled = [];
    private readonly List<StandupEvent> _events = [];
    private bool _rosterKnown;

    /// <summary>Gets the native retrospective phase derived by the existing agenda.</summary>
    public bool InActions => _events.Any(e => e.Type == StandupEventType.Phase && e.Text == RetroAgenda.ThemesMarker);

    /// <summary>Updates presence from the authoritative call roster, never a display-name match.</summary>
    /// <param name="present">Verified active member ids in the call.</param>
    public void SetParticipants(IReadOnlySet<string> present)
    {
        _rosterKnown = true;
        foreach (var member in session.PreBrief!.Members)
        {
            _attendance[member.MemberId] = present.Contains(member.MemberId) ? Attendance.Present : Attendance.Absent;
            _status[member.MemberId] = present.Contains(member.MemberId) ? null : "away";
        }
    }

    /// <summary>Checks current call presence without marking a member absent before the first snapshot.</summary>
    /// <param name="memberId">The roster member.</param>
    /// <returns>Whether the known call roster excludes the member.</returns>
    public bool IsAbsent(string memberId) => _rosterKnown && _attendance.GetValueOrDefault(memberId) == Attendance.Absent;

    /// <summary>Completes one turn in the ephemeral agenda.</summary>
    /// <param name="memberId">The previous member.</param>
    public void CompleteTurn(string? memberId)
    {
        if (memberId is not null) { _handled.Add(memberId); }
    }

    /// <summary>Gets the next retrospective step using the same pure agenda as the saved ceremony.</summary>
    /// <returns>The next step, or null to close.</returns>
    public AgendaStep? NextRetroStep() => RetroAgenda.Instance.Next(Snapshot());

    /// <summary>Records the agenda phase in memory so themes and action turns are not repeated.</summary>
    /// <param name="step">The selected step.</param>
    /// <param name="now">Event time.</param>
    public void SelectRetroStep(AgendaStep step, DateTimeOffset now)
    {
        var marker = step switch
        {
            Announcement announcement => announcement.Phase,
            MemberTurn { Phase: "Actions" } member => RetroAgenda.ActionTurnPrefix + member.Member.MemberId,
            _ => null,
        };
        if (marker is not null) { _events.Add(new StandupEvent(Guid.NewGuid(), now, StandupEventType.Phase, null, [], marker)); }
    }

    /// <summary>Applies transcript coverage and retro interpretation only to a verified member, in memory.</summary>
    /// <param name="memberId">The attributed member.</param>
    /// <param name="text">Transient transcript.</param>
    /// <param name="answering">Whether this answers the one clarifying question.</param>
    /// <param name="now">Event time.</param>
    public void ApplyTranscript(string memberId, string text, bool answering, DateTimeOffset now)
    {
        var member = session.PreBrief!.Members.FirstOrDefault(m => m.MemberId == memberId);
        if (member is null) { return; }
        if (text.Length > 4096) { text = text[..4096]; }
        if (session.Kind != CeremonyKind.Retrospective)
        {
            var known = session.PreBrief.Members.SelectMany(m => m.Items).Concat(session.PreBrief.UnassignedItems).Select(i => i.Id.Value).ToHashSet();
            var mentioned = ItemMentions.Detect(text, member.Items, known);
            _covered.UnionWith(mentioned);
            if (mentioned.Count > 0) { _focus[memberId] = mentioned[^1]; }
            return;
        }

        var interpretation = InActions
            ? RetroInterpreter.Action(memberId, text, answering, session.PreBrief.Members, _events)
            : RetroInterpreter.Round(memberId, text, answering, correction: false, _events);
        _events.RemoveAll(e => interpretation.RetractedEventIds.Contains(e.Id));
        foreach (var note in interpretation.Notes)
        {
            if (_events.Count(e => e.Type == StandupEventType.ActionItem) >= RetroAgenda.MaxActions && note.Type == StandupEventType.ActionItem) { break; }
            _events.Add(new StandupEvent(Guid.NewGuid(), now, note.Type, memberId, [], note.Text, Category: note.Category, OwnerMemberId: note.OwnerMemberId));
        }
    }

    /// <summary>Marks a deferred member using metadata; no speech text is saved.</summary>
    /// <param name="memberId">The member.</param>
    /// <param name="status">The status displayed on the board.</param>
    public void SetStatus(string memberId, string status) => _status[memberId] = status;

    /// <summary>Builds the complete shared-board protocol, including explicit ownership and non-recording state.</summary>
    /// <param name="currentMemberId">The floor's current member.</param>
    /// <param name="opened">Whether the ceremony has opened.</param>
    /// <param name="ended">Whether the ceremony has ended.</param>
    /// <returns>The transient board message.</returns>
    public object Project(string? currentMemberId, bool opened, bool ended)
    {
        var brief = session.PreBrief!;
        var speaker = brief.Members.FirstOrDefault(m => m.MemberId == currentMemberId);
        if (session.Kind == CeremonyKind.Retrospective)
        {
            var summary = RetroSummary.Build(Snapshot());
            return new
            {
                type = "board",
                teamId = session.TeamId,
                sessionId = session.Id,
                recording = false,
                opened,
                ended,
                kind = "retro",
                sprint = brief.IterationPath,
                phase = InActions ? "Actions" : "Round",
                speaker = speaker?.DisplayName,
                columns = new[] { RetroCategory.Continue, RetroCategory.Stop, RetroCategory.Start }.Select(category => new
                {
                    key = category.ToString(),
                    label = RetroFormats.Label(RetroFormat.WentWell, category),
                    notes = summary.In(category).Select(note => new { text = note.Text, member = brief.Members.FirstOrDefault(m => m.MemberId == note.MemberId)?.DisplayName }),
                }),
                themes = summary.Themes.Select(theme => new { name = theme.Name, count = theme.Count }),
                actions = summary.Actions.Select(action => new { text = action.Text, owner = action.OwnerName }),
                team = brief.Members.Select(m => new { name = m.DisplayName, status = _status.GetValueOrDefault(m.MemberId) }),
            };
        }

        return new
        {
            type = "board",
            teamId = session.TeamId,
            sessionId = session.Id,
            recording = false,
            opened,
            ended,
            sprint = brief.IterationPath,
            goal = brief.SprintGoal,
            daysLeft = brief.DaysLeft,
            speaker = speaker?.DisplayName,
            focus = speaker is not null && _focus.TryGetValue(speaker.MemberId, out var focus) ? focus : (int?)null,
            items = (speaker?.Items ?? brief.Members.SelectMany(m => m.Items).Where(i => i.Flags.Count > 0).ToList()).Select(item => new
            {
                id = item.Id.Value,
                owner = brief.Members.FirstOrDefault(m => m.Items.Any(i => i.Id == item.Id))?.DisplayName,
                title = item.Title,
                type = item.Type,
                state = item.State,
                flags = item.Flags.Select(flag => flag.ToString()),
                covered = _covered.Contains(item.Id.Value),
            }),
            team = brief.Members.Select(m => new
            {
                name = m.DisplayName,
                total = m.Items.Count,
                covered = m.Items.Count(i => _covered.Contains(i.Id.Value)),
                status = _status.GetValueOrDefault(m.MemberId),
            }),
        };
    }

    private StandupSession Snapshot() => StandupSession.Restore(new StandupSessionSnapshot(
        session.Id, session.TeamId, session.CreatedAt, StandupStatus.Live, session.PreBrief, session.Mode, joinUrl,
        session.StandupMinutes, session.StartedAt, null, _attendance, _handled.ToArray(), _events, [], session.Kind));
}
