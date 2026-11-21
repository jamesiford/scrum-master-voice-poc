using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Infrastructure.Demo;

namespace Mcm.Scrum.Infrastructure.Agents;

/// <summary>Loads prompts from <c>agents/instructions/{name}.md</c>, re-read on every call so edits apply without a rebuild.</summary>
public sealed class FilePromptStore : IPromptStore
{
    /// <inheritdoc />
    public async Task<string?> GetAsync(string name, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Any(c => !char.IsLetterOrDigit(c) && c != '-'))
        {
            return null;
        }

        var path = RepositoryFileLocator.Resolve(Path.Combine("agents", "instructions", name + ".md"));
        return path is null ? null : await File.ReadAllTextAsync(path, ct);
    }
}
