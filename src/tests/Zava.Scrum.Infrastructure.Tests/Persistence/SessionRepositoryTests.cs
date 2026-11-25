using Microsoft.Extensions.Options;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Infrastructure.Persistence;

namespace Zava.Scrum.Infrastructure.Tests.Persistence;

public sealed class SessionRepositoryTests : SessionRepositoryContract, IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "zava-scrum-tests", Guid.NewGuid().ToString("N"));

    protected override ISessionRepository CreateRepository() =>
        new SessionRepository(new FileSessionItemStore(Options.Create(new StoreOptions { DataPath = _dir })));

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }
}
