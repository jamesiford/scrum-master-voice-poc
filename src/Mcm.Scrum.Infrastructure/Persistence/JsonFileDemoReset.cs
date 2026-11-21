using Mcm.Scrum.Application.Abstractions.Ports;
using Microsoft.Extensions.Options;

namespace Mcm.Scrum.Infrastructure.Persistence;

/// <summary>Deletes the demo JSON files. Teams, roster, schedule and Teams conversations are kept so the demo can be re-run at once.</summary>
public sealed class JsonFileDemoReset(IOptions<StoreOptions> options) : IDemoReset
{
    private static readonly string[] Files = ["sessions.json", "followups.json", "cards.json", "bot-activities.json"];

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken ct)
    {
        foreach (var file in Files)
        {
            var path = Path.Combine(options.Value.DataPath, file);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        return Task.CompletedTask;
    }
}