using Microsoft.Extensions.Options;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Identity;
using Zava.Scrum.Infrastructure.Persistence;

namespace Zava.Scrum.Infrastructure.Tests.Persistence;

/// <summary>The file approval store is a JSON-lines file: one record per line, and every record reads back whole.</summary>
public sealed class FileApprovalRecordStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "zava-approvals-" + Guid.NewGuid().ToString("N"));
    private readonly FileApprovalRecordStore _store;

    public FileApprovalRecordStoreTests() =>
        _store = new FileApprovalRecordStore(Options.Create(new StoreOptions { DataPath = _dir }));

    [Fact]
    public async Task Append_ThenRead_ReturnsEveryFieldOfEveryRecord()
    {
        var followUp = Guid.NewGuid();
        var decision = Record(followUp, ApprovalRecordKind.Approved, null, []);
        var applied = Record(followUp, ApprovalRecordKind.Applied, decision.Id, [new AdoRevision(1, 7), new AdoRevision(2, 9)]);

        await _store.AppendAsync(decision, default);
        await _store.AppendAsync(applied, default);

        var byFollowUp = await _store.ListByFollowUpAsync(followUp, default);
        Assert.Equal(2, byFollowUp.Count);
        Assert.Equal(decision.Actor, byFollowUp[0].Actor);
        Assert.Equal(applied.DecisionRecordId, byFollowUp[1].DecisionRecordId);
        Assert.Equal([new AdoRevision(1, 7), new AdoRevision(2, 9)], byFollowUp[1].AdoRevisions);
        Assert.Equal(2, (await _store.ListByCorrelationAsync(decision.CorrelationId[..12], default)).Count);
    }

    [Fact]
    public async Task Append_WritesOneLinePerRecord()
    {
        await _store.AppendAsync(Record(Guid.NewGuid(), ApprovalRecordKind.Applied, Guid.NewGuid(), [new AdoRevision(1, 7)]), default);
        await _store.AppendAsync(Record(Guid.NewGuid(), ApprovalRecordKind.Approved, null, []), default);

        var lines = await File.ReadAllLinesAsync(Path.Combine(_dir, "approval-records.jsonl"));

        Assert.Equal(2, lines.Length);
        Assert.All(lines, l => Assert.StartsWith("{", l, StringComparison.Ordinal));
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    private static ApprovalRecord Record(Guid followUp, ApprovalRecordKind kind, Guid? decision, IReadOnlyList<AdoRevision> revisions) =>
        new(Guid.NewGuid(), "4bf92f3577b34da6a3ce929d0e0e4736", followUp, "team-payments", kind, "hash", Actors.Dana(ActorSource.Portal),
            ApprovalChannel.Portal, new DateTimeOffset(2026, 1, 8, 9, 0, 0, TimeSpan.Zero), AuthenticationContext.Of("entra-bearer"), decision, revisions);
}
