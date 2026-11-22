namespace Mcm.Scrum.Domain.Work;

/// <summary>An Azure DevOps iteration with its dates. The dates are dates: Azure DevOps stores no time of day for them.</summary>
/// <param name="Path">The iteration path.</param>
/// <param name="Start">First day.</param>
/// <param name="End">Last day.</param>
public sealed record IterationInfo(string Path, DateOnly Start, DateOnly End)
{
    /// <summary>Gets the short name, the last segment of the path (for example <c>Sprint 43</c>).</summary>
    public string Name => Path[(Path.LastIndexOf('\\') + 1)..];
}
