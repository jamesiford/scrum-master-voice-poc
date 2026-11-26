using Zava.Scrum.Domain.Flags;
using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Reports;

/// <summary>Computes Copilot selections and figures, reusing the sprint flag and work item id guards.</summary>
public static class CopilotFactsBuilder
{
    private const int MaxLines = 5;

    /// <summary>Builds a short answer from a delegated sprint and team-scoped sessions.</summary>
    /// <param name="team">The resolved membership team.</param>
    /// <param name="skill">The requested skill.</param>
    /// <param name="sprint">Only items readable by the asker.</param>
    /// <param name="member">The uniquely verified asker roster entry, if available.</param>
    /// <param name="sessions">Sessions from which only this team's metadata or the asker's commitments are selected.</param>
    /// <param name="now">Current time from the application clock.</param>
    /// <returns>Computed figures and bounded, guarded citations.</returns>
    public static CopilotFacts Build(Team team, CopilotSkill skill, SprintSnapshot sprint, Member? member, IReadOnlyList<StandupSession> sessions, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(team);
        ArgumentNullException.ThrowIfNull(sprint);
        ArgumentNullException.ThrowIfNull(sessions);
        member = member?.TeamId == team.Id ? member : null;
        var ownSessions = sessions.Where(s => s.TeamId == team.Id && s.Kind == CeremonyKind.DailyScrum);
        if (skill == CopilotSkill.StandupStatus)
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(team.TimeZone);
            var today = TimeZoneInfo.ConvertTime(now, zone).Date;
            var latest = ownSessions.Where(s => TimeZoneInfo.ConvertTime(s.StartedAt ?? s.CreatedAt, zone).Date == today)
                .OrderByDescending(s => s.StartedAt ?? s.CreatedAt).FirstOrDefault();
            return new(team.Id, latest is null ? "No standup is prepared for today." : $"Today's standup is {latest.Status}.", [], []);
        }

        var known = sprint.Items.Select(i => i.Id).ToHashSet();
        if (skill == CopilotSkill.Commitments)
        {
            var commitments = member is null ? [] : ownSessions.SelectMany(s => s.Events)
                .Where(e => !e.Retracted && e.Type == StandupEventType.Commitment && e.MemberId == member.Id &&
                    e.WorkItemIds.Count > 0 && e.WorkItemIds.All(id => known.Contains(new WorkItemId(id))) &&
                    WorkItemIdGuard.IsGrounded(e.Text, known))
                .OrderByDescending(e => e.At).ToList();
            var shown = commitments.Take(MaxLines).ToList();
            return new(team.Id, $"You have {commitments.Count} recorded commitments linked to readable current-sprint items; showing {shown.Count}.",
                shown.Select(e => $"{string.Join(", ", e.WorkItemIds.Select(id => $"#{id}"))}: {e.Text}").ToList(),
                shown.SelectMany(e => e.WorkItemIds).Distinct().Select(id => new WorkItemId(id)).ToList());
        }

        var flags = FlagEvaluator.CreateDefault();
        var context = new FlagContext(now, team.Behaviours);
        var selected = sprint.Items.Where(i => skill switch
        {
            CopilotSkill.Blocked => flags.Evaluate(i, context).Contains(Flag.Blocked),
            CopilotSkill.SprintRisk => flags.Evaluate(i, context).Any(f => f is Flag.AtRisk or Flag.Overdue),
            CopilotSkill.MyItems => member is not null && !string.IsNullOrWhiteSpace(member.AdoUniqueName) &&
                string.Equals(i.AssignedTo, member.AdoUniqueName, StringComparison.OrdinalIgnoreCase),
            _ => false,
        }).OrderBy(i => i.Id.Value).ToList();
        var items = selected.Take(MaxLines).ToList();
        var label = skill switch
        {
            CopilotSkill.Blocked => "blocked",
            CopilotSkill.SprintRisk => "at risk or overdue",
            _ => "assigned to you",
        };
        return new(team.Id, $"{selected.Count} readable sprint {(selected.Count == 1 ? "item is" : "items are")} {label}; showing {items.Count}.",
            items.Select(i => $"#{i.Id.Value}: {SafeText(i.Title, known, "Work item")} - {SafeText(i.State, known, "State unavailable")}").ToList(), items.Select(i => i.Id).ToList());
    }

    private static string SafeText(string text, IReadOnlySet<WorkItemId> known, string fallback) =>
        WorkItemIdGuard.IsGrounded(text, known) ? text.Replace('\n', ' ').Replace('\r', ' ') : fallback;
}
