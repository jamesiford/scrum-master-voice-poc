using System.Globalization;
using Mcm.Scrum.Domain.FollowUps;
using Mcm.Scrum.Domain.Standups;
using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Domain.Ceremonies;

/// <summary>Builds the sprint review pack: what was completed, the points delivered against the commitment, and a note on each finished item.</summary>
public static class ReviewPackBuilder
{
    /// <summary>Builds the pack.</summary>
    /// <param name="sprint">The current sprint.</param>
    /// <returns>The pack.</returns>
    public static CeremonyPack Build(SprintSnapshot sprint)
    {
        ArgumentNullException.ThrowIfNull(sprint);
        var stories = sprint.Items.Where(i => i.Type is "User Story" or "Bug").ToList();
        var done = stories.Where(i => i.IsDone).ToList();
        var committed = stories.Sum(i => i.StoryPoints ?? 0);
        var delivered = done.Sum(i => i.StoryPoints ?? 0);
        var open = sprint.Items.Count(i => !i.IsDone);
        var items = done.Select(i => new PackItem(
            $"Note on #{i.Id} {i.Title}",
            new ProposedChange(i.Id.Value, ChangeOp.Comment, $"Completed in {sprint.IterationPath} and shown in the sprint review.", "Finished this sprint."))).ToList();
        var facts = new List<PackFact>
        {
            new("Delivered", $"{delivered:0.#} of {committed:0.#} points"),
            new("Completed stories and bugs", done.Count.ToString(CultureInfo.InvariantCulture)),
            new("Still open", open.ToString(CultureInfo.InvariantCulture)),
        };
        var summary = $"{delivered:0.#} of {committed:0.#} points were delivered across {done.Count} stories and bugs; {open} items are still open.";
        return new CeremonyPack(CeremonyKind.SprintReview, sprint.IterationPath, summary, facts, items);
    }
}
