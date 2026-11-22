using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace Mcm.Scrum.Infrastructure.Tests.Persistence;

public sealed class JsonFileSessionRepositoryTests : SessionRepositoryContract, IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "mcm-scrum-tests", Guid.NewGuid().ToString("N"));

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
