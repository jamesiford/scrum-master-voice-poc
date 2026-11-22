using System.Globalization;
using Mcm.Scrum.Domain.FollowUps;
using Mcm.Scrum.Domain.Members;
using Mcm.Scrum.Domain.Planning;
using Mcm.Scrum.Domain.Standups;
using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Domain.Ceremonies;

/// <summary>Builds the retrospective pack: the start-stop-continue figures and the follow-up tasks that would be created.</summary>
public static class RetroPackBuilder
{
    /// <summary>The most actions proposed from one retrospective.</summary>
    public const int MaxActions = 3;

    private const int MaxTitle = 100;

    /// <summary>Builds the pack.</summary>
    /// <param name="sprint">The current sprint.</param>
    /// <param name="sessions">The team's sessions, newest first.</param>
    /// <param name="roster">The roster.</param>
    /// <param name="nextIterationPath">The iteration new actions go to; the current one when <see langword="null"/>.</param>
    /// <returns>The pack.</returns>
    public static CeremonyPack Build(SprintSnapshot sprint, IReadOnlyList<StandupSession> sessions, IReadOnlyList<Member> roster, string? nextIterationPath)
    {
        ArgumentNullException.ThrowIfNull(sprint);
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(roster);
        var draft = RetrospectiveBuilder.Build(sprint, sessions, PlanningPlanner.Plan(sprint, roster));
        var iteration = nextIterationPath ?? sprint.IterationPath;
        var items = draft.Start.Take(MaxActions).Select(line => new PackItem(
            $"Create task: {line}",
            ProposedChange.Create(new NewWorkItem("Task", Title(line), line, ["retro-action"], iteration, null), "Proposed in the retrospective."))).ToList();

        var events = sessions.SelectMany(s => s.ActiveEvents).ToList();
        var facts = new List<PackFact>
        {
            new("Items done", $"{sprint.Items.Count(i => i.IsDone)} of {sprint.Items.Count}"),
            new("Blockers raised in standups", events.Count(e => e.Type == StandupEventType.Blocker).ToString(CultureInfo.InvariantCulture)),
            new("Topics parked", events.Count(e => e.Type == StandupEventType.ParkingLot).ToString(CultureInfo.InvariantCulture)),
        };
        var summary = $"{draft.Start.Count} things to start, {draft.Stop.Count} to stop and {draft.Continue.Count} to continue; {items.Count} actions proposed.";
        return new CeremonyPack(CeremonyKind.Retrospective, iteration, summary, facts, items);
    }

    private static string Title(string line) => "Retro action: " + (line.Length <= MaxTitle ? line : line[..MaxTitle]);
}
