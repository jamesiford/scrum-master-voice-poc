using System.Text.Json;
using Microsoft.Extensions.Options;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>
/// The approval trail as an append-only JSON-lines file (<c>Store__Mode=File</c>). Records are only ever appended; nothing rewrites or
/// removes a line.
/// </summary>
public sealed class FileApprovalRecordStore(IOptions<StoreOptions> options) : IApprovalRecordStore, IDisposable
{
    private static readonly JsonSerializerOptions Json = JsonFile.SerializerOptions;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path = Path.Combine(options.Value.DataPath, "approval-records.jsonl");

    /// <inheritdoc />
    public async Task AppendAsync(ApprovalRecord record, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(record);
        await _gate.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            await File.AppendAllTextAsync(_path, JsonSerializer.Serialize(record, Json) + "\n", ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApprovalRecord>> ListByFollowUpAsync(Guid followUpId, CancellationToken ct) =>
        (await ReadAllAsync(ct)).Where(r => r.FollowUpId == followUpId).ToList();

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApprovalRecord>> ListByCorrelationAsync(string correlationPrefix, CancellationToken ct) =>
        (await ReadAllAsync(ct)).Where(r => r.CorrelationId.StartsWith(correlationPrefix, StringComparison.Ordinal)).ToList();

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private async Task<List<ApprovalRecord>> ReadAllAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (!File.Exists(_path))
            {
                return [];
            }

            var lines = await File.ReadAllLinesAsync(_path, ct);
            return lines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => JsonSerializer.Deserialize<ApprovalRecord>(l, Json)!).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }
}
