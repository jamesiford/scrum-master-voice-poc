using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Health.GetHealth;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.Standups;
using Microsoft.Extensions.DependencyInjection;

namespace Zava.Scrum.Application.Tests.Features;

public sealed class GetHealthHandlerTests
{
    [Fact]
    public async Task Send_GetHealthQuery_ReturnsHealthyWithSessionCount()
    {
        var now = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);
        var repository = new InMemorySessionRepository();
        await repository.SaveAsync(StandupSession.Prepare(Guid.NewGuid(), "t1", now, null), CancellationToken.None);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<ISessionRepository>(repository);
        services.AddSingleton<IClock>(new FixedClock(now));
        using var sp = services.BuildServiceProvider();

        var result = await sp.GetRequiredService<ISender>().Send(new GetHealthQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(new HealthDto("healthy", now, 1), result.Value);
    }
}
