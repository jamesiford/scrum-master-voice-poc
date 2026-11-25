using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure.Tests.Persistence;

public sealed class JsonFileSessionRepositoryTests : SessionRepositoryContract, IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "zava-scrum-tests", Guid.NewGuid().ToString("N"));

    protected override ISessionRepository CreateRepository() =>
        new JsonFileSessionRepository(new FileCollectionStore(Options.Create(new StoreOptions { DataPath = _dir })));

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }
}
