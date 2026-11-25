using Azure.Core;
using Microsoft.Extensions.Options;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Flags;
using Zava.Scrum.Domain.Kpis;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Infrastructure.Agents;
using Zava.Scrum.Infrastructure.Persistence;
using Zava.Scrum.Infrastructure.Voice;

namespace Zava.Scrum.AgentHost.Tests;

/// <summary>
/// Kill the host mid-standup and resume (ADR 0021, prompt 4 step 2): the voice connection drops, the session is read back from the store with
/// every event recorded before the kill, the caller reconnects to the same hosted session id and the conversation carries on.
/// </summary>
public sealed class HostRestartTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "zava-scrum-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task KillingTheHostMidStandup_ThenReconnecting_ResumesFromTheStoredSessionAndTheVoiceCarriesOn()
    {
        var upstreamPort = LoopbackServers.FreePort();
        var hostPort = LoopbackServers.FreePort();
        var received = new List<string>();
        await using var fake = await LoopbackServers.FakeVoiceLiveAsync(upstreamPort, received);
        var host = await LoopbackServers.RelayHostAsync(hostPort, upstreamPort);
        var repository = new SessionRepository(new FileSessionItemStore(Options.Create(new StoreOptions { DataPath = _dir })));
        var session = Live("team-payments");
        session.RecordEvent(StandupEventType.Commitment, "m1", [4521], "I will finish the retry work", DateTimeOffset.UtcNow);
        await repository.SaveAsync(session, default);
        var factory = new VoiceLiveSessionFactory(
            new FakeCredential(),
            Options.Create(new VoiceLiveOptions { RelayEndpoint = $"ws://127.0.0.1:{hostPort}/relay", Model = "gpt-realtime-2.1" }),
            Options.Create(new FoundryOptions { ProjectEndpoint = "https://example/api/projects/p" }));
        var config = new VoiceSessionConfig("persona", [], 800, session.TeamId, session.Id);

        await using (var first = await factory.CreateAsync(config, default))
        {
            await first.SendAudioAsync(new byte[960], default);
            Assert.IsType<UserSpeechStarted>(await NextAsync(first));

            // The host is killed with the connection open.
            await host.StopAsync();
            await host.DisposeAsync();
            Assert.IsType<VoiceFault>(await NextAsync(first));
        }

        host = await LoopbackServers.RelayHostAsync(hostPort, upstreamPort);
        await using var restarted = host;
        var resumed = await repository.GetAsync(session.Id, default);
        await using var second = await factory.CreateAsync(config, default);
        await second.SendAudioAsync(new byte[960], default);

        Assert.IsType<UserSpeechStarted>(await NextAsync(second));
        Assert.NotNull(resumed);
        Assert.Equal(session.Id, resumed.Id);
        Assert.Equal(StandupStatus.Live, resumed.Status);
        Assert.Equal("I will finish the retry work", resumed.Events.Single(e => e.Type == StandupEventType.Commitment).Text);
        Assert.Equal(2, received.Count(m => m.Contains("session.update", StringComparison.Ordinal)));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    private static async Task<VoiceEvent> NextAsync(IVoiceSession session)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await foreach (var e in session.ReadEventsAsync(cts.Token))
        {
            return e;
        }

        throw new InvalidOperationException("The voice session closed without an event.");
    }

    private static StandupSession Live(string team)
    {
        var brief = new PreBrief(team, "p\\s", "Goal", 6, new Burndown(40, 14, 26, 6, true), [], [], ["risk"], "Summary");
        var session = StandupSession.Prepare(Guid.NewGuid(), team, DateTimeOffset.UtcNow, brief);
        session.Start("Simulated", null, 15, DateTimeOffset.UtcNow);
        return session;
    }

    private sealed class FakeCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) => new("token", DateTimeOffset.UtcNow.AddHours(1));

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) => new(GetToken(requestContext, cancellationToken));
    }
}
