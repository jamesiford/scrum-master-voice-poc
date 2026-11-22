using Zava.Scrum.Domain.Flags;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Domain.FollowUps;

/// <summary>
/// Turns what happened in a standup into follow-ups: one per member with something to confirm, one per absent member,
/// and an escalation for each blocker that has lasted too long. Pure rules, no model, so the cards are predictable (rule 3).
/// </summary>
public static class FollowUpPlanner
{
    /// <summary>Plans the follow-ups for a finished standup.</summary>
    /// <param name="session">The standup (must have a pre-brief).</param>
    /// <param name="escalateBlockerAfterDays">A blocker older than this many days is escalated.</param>
    /// <param name="newId">Creates ids (keeps this class free of ambient state).</param>
    /// <param name="now">Current time.</param>
    /// <returns>The follow-ups, unsaved.</returns>
    public static IReadOnlyList<FollowUp> Plan(StandupSession session, int escalateBlockerAfterDays, Func<Guid> newId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(newId);
        if (session.PreBrief is null)
        {
            return [];
        }

        var date = DateOnly.FromDateTime((session.StartedAt ?? session.CreatedAt).UtcDateTime);
        var result = new List<FollowUp>();
        foreach (var member in session.PreBrief.Members)
        {
            if (session.Attendance.TryGetValue(member.MemberId, out var a) && a == Attendance.Absent)
            {
                result.Add(FollowUp.Create(newId(), session.TeamId, session.Id, member.MemberId, FollowUpKind.Absent, AbsentMessage(member), [], date, now));
                continue;
            }

            if (session.NoResponseCount(member.MemberId) > 0 && !session.HasSpoken(member.MemberId))
            {
                // Present but silent is not the same as absent: the message says so.
                result.Add(FollowUp.Create(newId(), session.TeamId, session.Id, member.MemberId, FollowUpKind.Absent, NoResponseMessage(member), [], date, now));
                continue;
            }

            var changes = ChangesFor(session, member);
            if (changes.Count > 0)
            {
                result.Add(FollowUp.Create(newId(), session.TeamId, session.Id, member.MemberId, FollowUpKind.Followup, FollowupMessage(member, changes), changes, date, now));
            }
        }

        result.AddRange(Unblocks(session, newId, date, now));
        result.AddRange(Escalations(session, escalateBlockerAfterDays, newId, date, now));
        result.AddRange(MissedCommitments(session, newId, date, now));
        return result;
    }

    private static List<ProposedChange> ChangesFor(StandupSession session, MemberBrief member) =>
        session.ActiveEvents
            .Where(e => e.MemberId == member.MemberId && e.Type is StandupEventType.Blocker or StandupEventType.Commitment && e.WorkItemIds.Count > 0)
            .Select(e => new ProposedChange(
                e.WorkItemIds[0],
                ChangeOp.Comment,
                $"{(e.Type == StandupEventType.Blocker ? "Blocker raised" : "Commitment")} in standup: {e.Text}",
                $"{member.DisplayName} said this in the standup."))
            .DistinctBy(c => (c.WorkItemId, c.Value))
            .ToList();

    private static string FollowupMessage(MemberBrief member, List<ProposedChange> changes) =>
        $"Thanks {Name(member)}. Here is what I captured from today's standup. Approve to add it to Azure DevOps, or reject to leave it out.{Environment.NewLine}"
        + string.Join(Environment.NewLine, changes.Select(c => $"- Work item {c.WorkItemId}: {c.Value}"));

    private static string AbsentMessage(MemberBrief member) =>
        $"Hi {Name(member)}, you were not at today's standup. What did you finish, what is next, and is anything blocking you?";

    private static string NoResponseMessage(MemberBrief member) =>
        $"Hi {Name(member)}, you were in today's standup but I did not hear an update from you. What did you finish, what is next, and is anything blocking you?";

    // M9-R2: a blocker with a named unblocker becomes a follow-up for that teammate, with the item, who is blocked and what was said.
    private static IEnumerable<FollowUp> Unblocks(StandupSession session, Func<Guid> newId, DateOnly date, DateTimeOffset now)
    {
        var members = session.PreBrief!.Members.ToDictionary(m => m.MemberId);
        foreach (var blocker in session.ActiveEvents.Where(e => e.Type == StandupEventType.Blocker && e.UnblockerMemberId is not null && e.MemberId is not null))
        {
            if (!members.TryGetValue(blocker.UnblockerMemberId!, out var helper) || !members.TryGetValue(blocker.MemberId!, out var blocked))
            {
                continue;
            }

            var item = blocker.WorkItemIds.Count > 0 ? blocked.Items.FirstOrDefault(i => i.Id.Value == blocker.WorkItemIds[0]) : null;
            var about = item is null ? "an item" : $"{item.Type} {item.Id} ({item.Title})";
            var message = $"Hi {Name(helper)}, {Name(blocked)} is blocked on {about} and said you may be able to help: \"{blocker.Text.Trim()}\". Can you pick this up with them?";
            yield return FollowUp.Create(newId(), session.TeamId, session.Id, helper.MemberId, FollowUpKind.Unblock, message, [], date, now);
        }
    }

    // A blocker that has lasted too long is escalated whether or not it was raised again today (M9-R2).
    private static IEnumerable<FollowUp> Escalations(StandupSession session, int afterDays, Func<Guid> newId, DateOnly date, DateTimeOffset now)
    {
        var items = session.PreBrief!.Members.SelectMany(m => m.Items.Select(i => (Member: m, Item: i)));
        foreach (var (member, item) in items.Where(x => x.Item.Flags.Contains(Flag.Blocked) && x.Item.DaysInState >= afterDays))
        {
            var message = $"{item.Type} {item.Id} ({item.Title}), owned by {Name(member)}, has been blocked for {item.DaysInState} days. It needs your help.";
            yield return FollowUp.Create(newId(), session.TeamId, session.Id, null, FollowUpKind.Escalation, message, [], date, now, session.Kind, forScrumMaster: true);
        }
    }

    // M9-R3: a commitment still open two standups later goes to the Scrum Master.
    private static IEnumerable<FollowUp> MissedCommitments(StandupSession session, Func<Guid> newId, DateOnly date, DateTimeOffset now)
    {
        foreach (var member in session.PreBrief!.Members)
        {
            foreach (var missed in (member.PreviousCommitments ?? []).Where(c => c.IsMissed))
            {
                var item = member.Items.FirstOrDefault(i => i.Id.Value == missed.WorkItemId && i.Flags.Contains(Flag.MissedCommitment));
                if (item is null)
                {
                    continue;
                }

                var message = $"{Name(member)} said \"{missed.Text}\" on {item.Type} {item.Id} ({item.Title}) {missed.StandupsAgo} standups ago and it is still {item.State}. Missed commitment.";
                yield return FollowUp.Create(newId(), session.TeamId, session.Id, null, FollowUpKind.Escalation, message, [], date, now, session.Kind, forScrumMaster: true);
            }
        }
    }
    private static string Name(MemberBrief member)
    {
        var space = member.DisplayName.IndexOf(' ', StringComparison.Ordinal);
        return space < 0 ? member.DisplayName : member.DisplayName[..space];
    }
}
