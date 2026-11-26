using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Zava.Scrum.Api.Voice;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Api.Tests;

public sealed class MediaHostAuthorizationTests
{
    [Theory]
    [InlineData("wrong-team", true, false, HttpStatusCode.Forbidden)]
    [InlineData("team-one", false, false, HttpStatusCode.Unauthorized)]
    [InlineData("team-one", true, true, HttpStatusCode.Unauthorized)]
    public async Task Context_MismatchedTeamOrCapability_DisclosesNoMeetingLink(string team, bool key, bool wrongSession, HttpStatusCode expected)
    {
        using var root = new ApiFactory();
        using var factory = root.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Bridge:Mode", "GraphMedia");
            builder.UseSetting("Bridge:GraphMediaBaseUrl", "https://media.example");
            builder.UseSetting("Bridge:ApiKey", "test-control-key");
            builder.UseSetting("VoiceLive:RelayEndpoint", "wss://relay.example/invocations_ws");
        });
        using var scope = factory.Services.CreateScope();
        var session = StandupSession.Prepare(Guid.NewGuid(), "team-one", DateTimeOffset.UnixEpoch, null);
        session.Start("acs", "https://teams.microsoft.com/meet/123", 15, DateTimeOffset.UnixEpoch);
        await scope.ServiceProvider.GetRequiredService<ISessionRepository>().SaveAsync(session, CancellationToken.None);
        using var client = factory.CreateClient();
        if (key) { client.DefaultRequestHeaders.Add("x-bridge-key", "test-control-key"); }
        var token = scope.ServiceProvider.GetRequiredService<BridgeTokenService>().Issue(wrongSession ? Guid.NewGuid() : session.Id);
        client.DefaultRequestHeaders.Add("x-bridge-token", token);

        using var response = await client.GetAsync($"/api/media/context?sessionId={session.Id}&teamId={team}");

        Assert.Equal(expected, response.StatusCode);
        Assert.DoesNotContain(session.JoinUrl!, await response.Content.ReadAsStringAsync());
    }
}
