namespace Zava.Scrum.Infrastructure.Demo;

/// <summary>Serializes the single PoC host's approved ADO writes with reset staging, so an in-flight ordinary write cannot pass the staged guard.</summary>
internal sealed class DemoAdoResetGate : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Runs one guarded ADO operation or reset-publication transition.</summary>
    public async Task<T> RunAsync<T>(Func<Task<T>> run, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            return await run();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();
}
