using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Domain.Teams;

/// <summary>
/// The rule that keeps a team's approved change inside the team (ADR 0018): a work item may only be written when it lives in the team's
/// project and under the team's area path. A change aimed at another team's item is refused, even when someone approved it.
/// </summary>
public static class WorkItemScope
{
    /// <summary>Gets the error for a write aimed outside the team's project and area path.</summary>
    /// <param name="workItemId">The item.</param>
    /// <returns>The error.</returns>
    public static Error OutOfScope(int workItemId) =>
        Error.Forbidden("team.work_item_out_of_scope", $"Work item {workItemId} is not in this team's project and area path, so it will not be written.");

    /// <summary>Checks an existing item.</summary>
    /// <param name="ado">The team's Azure DevOps connection.</param>
    /// <param name="project">The item's project (<c>System.TeamProject</c>).</param>
    /// <param name="areaPath">The item's area path (<c>System.AreaPath</c>).</param>
    /// <returns><see langword="true"/> when the project matches and the area path is the team's or beneath it.</returns>
    public static bool Contains(AdoSettings ado, string? project, string? areaPath)
    {
        ArgumentNullException.ThrowIfNull(ado);
        if (string.IsNullOrWhiteSpace(project) || string.IsNullOrWhiteSpace(areaPath))
        {
            return false;
        }

        if (!string.Equals(project, ado.Project, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var root = ado.AreaPath.TrimEnd('\\');
        var item = areaPath.TrimEnd('\\');
        return string.Equals(item, root, StringComparison.OrdinalIgnoreCase)
            || item.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Checks the iteration path of an item to be created: its first segment is its project.</summary>
    /// <param name="ado">The team's Azure DevOps connection.</param>
    /// <param name="iterationPath">The new item's iteration path.</param>
    /// <returns><see langword="true"/> when it belongs to the team's project.</returns>
    public static bool ContainsNew(AdoSettings ado, string iterationPath)
    {
        ArgumentNullException.ThrowIfNull(ado);
        return !string.IsNullOrWhiteSpace(iterationPath)
            && string.Equals(iterationPath.Split('\\')[0], ado.Project, StringComparison.OrdinalIgnoreCase);
    }
}
