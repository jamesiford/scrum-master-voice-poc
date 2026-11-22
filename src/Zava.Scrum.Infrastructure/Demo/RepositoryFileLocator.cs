namespace Mcm.Scrum.Infrastructure.Demo;

/// <summary>Finds repo files (seed data, prompts) by walking up from the working directory, so the API runs from any project folder.</summary>
public static class RepositoryFileLocator
{
    /// <summary>Resolves a repo-relative path to an existing file or directory.</summary>
    /// <param name="relativePath">For example <c>demo/seed-ado.json</c>.</param>
    /// <returns>The absolute path, or <see langword="null"/> when not found.</returns>
    public static string? Resolve(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        if (Path.IsPathRooted(relativePath))
        {
            return File.Exists(relativePath) || Directory.Exists(relativePath) ? relativePath : null;
        }

        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, relativePath);
                if (File.Exists(candidate) || Directory.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}
