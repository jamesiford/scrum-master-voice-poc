using Zava.Scrum.Application.Abstractions.Ports;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Deletes the demo collections. Teams, roster, schedule and Teams conversations are kept so the demo can be re-run at once.</summary>
public sealed class JsonFileDemoReset(ICollectionStore store, ISessionItemStore sessions) : IDemoReset
{
    private static readonly string[] Collections = ["followups.json", "cards.json", "bot-activities.json", "planning-packs.json", "sprint-calendars.json"];

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken ct)
    {
        await sessions.DeleteAllAsync(ct);
        foreach (var name in Collections)
        {
            await store.DeleteAsync(name, ct);
        }
    }
}
