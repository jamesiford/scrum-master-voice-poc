using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Zava.Scrum.Application;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Standups.JoinMeeting;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Infrastructure.Teams;

namespace Zava.Scrum.Infrastructure.Tests.Teams;

public sealed class GraphMediaBridgeTests
{
    [Fact]
    public async Task JoinMeeting_ThroughRealMediator_CarriesStoredTeamAndSessionToHost()
    {
        var session = StandupSession.Prepare(Guid.NewGuid(), "team-not-payments", DateTimeOffset.UnixEpoch, null);
        session.Start("acs", "https://teams.microsoft.com/meet/123", 15, DateTimeOffset.UnixEpoch);
        var sessions = new SessionStore(session);
        using var transport = new CapturingTransport();
        using var http = new HttpClient(transport) { BaseAddress = new Uri("https://media.example/") };
        var adapter = new GraphMediaBridge(http, sessions, Options.Create(new BridgeOptions { ApiKey = "test-key" }), NullLogger<GraphMediaBridge>.Instance);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<ISessionRepository>(sessions);
        services.AddSingleton<IMeetingBridge>(adapter);
        await using var provider = services.BuildServiceProvider();

        var result = await provider.GetRequiredService<ISender>().Send(new JoinMeetingCommand(session.Id, "session-token", "https://api.example"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        using var body = JsonDocument.Parse(transport.Body!);
        Assert.Equal(session.TeamId, body.RootElement.GetProperty("teamId").GetString());
        Assert.Equal(session.Id, body.RootElement.GetProperty("sessionId").GetGuid());
        Assert.Equal(session.JoinUrl, body.RootElement.GetProperty("joinUrl").GetString());
        Assert.Equal("test-key", transport.Key);
    }

    [Fact]
    public async Task JoinAsync_ChangedMeetingLink_RejectsBeforeNetwork()
    {
        var session = StandupSession.Prepare(Guid.NewGuid(), "team-one", DateTimeOffset.UnixEpoch, null);
        session.Start("acs", "https://teams.microsoft.com/meet/123", 15, DateTimeOffset.UnixEpoch);
        using var transport = new CapturingTransport();
        using var http = new HttpClient(transport);
        var adapter = new GraphMediaBridge(http, new SessionStore(session), Options.Create(new BridgeOptions()), NullLogger<GraphMediaBridge>.Instance);

        var result = await adapter.JoinAsync(new MeetingJoinRequest(session.Id, "https://teams.microsoft.com/meet/456", "token", "https://api.example", "agent"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(transport.Body);
    }

    [Fact]
    public async Task LeaveAsync_HostRejectsDelete_SurfacesFailure()
    {
        var session = StandupSession.Prepare(Guid.NewGuid(), "team-one", DateTimeOffset.UnixEpoch, null);
        using var transport = new CapturingTransport { Status = HttpStatusCode.ServiceUnavailable };
        using var http = new HttpClient(transport) { BaseAddress = new Uri("https://media.example/") };
        var adapter = new GraphMediaBridge(http, new SessionStore(session), Options.Create(new BridgeOptions { ApiKey = "key" }), NullLogger<GraphMediaBridge>.Instance);
        await Assert.ThrowsAsync<HttpRequestException>(() => adapter.LeaveAsync(session.Id, CancellationToken.None));
        Assert.Equal("key", transport.Key);
    }

    private sealed class SessionStore(StandupSession? session) : ISessionRepository
    {
        public Task<StandupSession?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(session?.Id == id ? session : null);

        public Task<IReadOnlyList<StandupSession>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<StandupSession>>(session is null ? [] : [session]);

        public Task SaveAsync(StandupSession value, CancellationToken ct) => throw new InvalidOperationException("This adapter only reads session state.");
    }

    private sealed class CapturingTransport : HttpMessageHandler
    {
        public string? Body { get; private set; }

        public string? Key { get; private set; }

        public HttpStatusCode Status { get; init; } = HttpStatusCode.Accepted;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Key = request.Headers.GetValues("x-bridge-key").Single();
            return new HttpResponseMessage(Status);
        }
    }
}
