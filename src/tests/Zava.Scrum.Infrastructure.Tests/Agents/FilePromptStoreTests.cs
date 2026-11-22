using Zava.Scrum.Infrastructure.Agents;
using Zava.Scrum.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure.Tests.Agents;

public sealed class FilePromptStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "prompts-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAsync_ThenNewStoreInstance_ReturnsOverride()
    {
        var options = Options.Create(new StoreOptions { DataPath = _dir });
        await new FilePromptStore(new FileCollectionStore(options)).SaveAsync("facilitator", "edited text", CancellationToken.None);

        var text = await new FilePromptStore(new FileCollectionStore(options)).GetAsync("facilitator", CancellationToken.None);

        Assert.Equal("edited text", text);
    }

    [Fact]
    public async Task SaveAsync_InvalidName_ReturnsFalse()
    {
        var store = new FilePromptStore(new FileCollectionStore(Options.Create(new StoreOptions { DataPath = _dir })));

        Assert.False(await store.SaveAsync("../evil", "x", CancellationToken.None));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }
}
