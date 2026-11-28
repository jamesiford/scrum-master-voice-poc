using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Domain.Teams;

/// <summary>Fails closed unless every explicitly selected team is demo-marked in the configured tenant.</summary>
public static class DemoResetScope
{
    /// <summary>Checks the full destructive scope before staging any reset.</summary>
    /// <param name="tenant">Configured demo tenant.</param>
    /// <param name="callerTenant">Validated caller tenant.</param>
    /// <param name="allowedIds">Explicit configuration allowlist.</param>
    /// <param name="teams">Teams being reset.</param>
    /// <returns>Success only for a complete, explicitly demo-marked scope.</returns>
    public static Result Check(Guid tenant, Guid callerTenant, IReadOnlyCollection<string> allowedIds, IReadOnlyList<Team> teams)
    {
        ArgumentNullException.ThrowIfNull(allowedIds);
        ArgumentNullException.ThrowIfNull(teams);
        return tenant != Guid.Empty && tenant == callerTenant && teams.Count > 0
            && teams.All(t => t.IsDemo && t.DemoTenantId == tenant && allowedIds.Contains(t.Id, StringComparer.Ordinal))
            ? Result.Success()
            : Result.Failure(Error.Forbidden("demo.reset_scope_refused", "Every selected team must be explicitly demo-marked, allowlisted, and in the configured demo tenant."));
    }

    /// <summary>Refuses ADO resets when an unselected team shares or overlaps the target's underlying work-item area.</summary>
    /// <param name="target">The team whose existing ADO seed items would be restored.</param>
    /// <param name="scope">The fully checked explicitly selected demo teams.</param>
    /// <param name="registry">All known teams, including paused or archived teams.</param>
    /// <returns>Success only when no unselected team can share the reset's ADO work.</returns>
    public static Result CheckIsolation(Team target, IReadOnlyList<Team> scope, IReadOnlyList<Team> registry)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(registry);
        var ids = scope.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        var shared = registry.Any(team => !ids.Contains(team.Id)
            && string.Equals(Organisation(team.Ado.Org), Organisation(target.Ado.Org), StringComparison.OrdinalIgnoreCase)
            && string.Equals(team.Ado.Project, target.Ado.Project, StringComparison.OrdinalIgnoreCase)
            && (Within(team.Ado.AreaPath, target.Ado.AreaPath) || Within(target.Ado.AreaPath, team.Ado.AreaPath)));
        return shared
            ? Result.Failure(Error.Forbidden("demo.shared_ado_scope", "An unselected team overlaps this demo's Azure DevOps area. Nothing may be reset until the underlying work is isolated or the full scope is explicitly demo-marked."))
            : Result.Success();
    }

    private static bool Within(string path, string root) =>
        string.Equals(path.TrimEnd('\\'), root.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(root.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase);

    private static string Organisation(string url)
    {
        var normal = AdoOrganisation.Normalise(url) ?? url.TrimEnd('/');
        return Uri.TryCreate(normal, UriKind.Absolute, out var uri) && uri.Host.EndsWith(".visualstudio.com", StringComparison.OrdinalIgnoreCase)
            ? "https://dev.azure.com/" + uri.Host[..^".visualstudio.com".Length]
            : normal;
    }
}
