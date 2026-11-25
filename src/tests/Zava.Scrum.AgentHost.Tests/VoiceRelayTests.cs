using System.Net.WebSockets;
using Zava.Scrum.AgentHost.Voice;

namespace Zava.Scrum.AgentHost.Tests;

/// <summary>The relay forwards frames unchanged, refuses a call with no team, and holds no state a restart could lose (ADR 0021).</summary>
public sealed class VoiceRelayTests
{
    [Fact]
    public async Task Relay_ForwardsTextAndBinaryFramesBothWaysUnchanged()
    {
        var received = new List<string>();
        var upstreamPort = LoopbackServers.FreePort();
        var hostPort = LoopbackServers.FreePort();
        await using var fake = await LoopbackServers.FakeVoiceLiveAsync(upstreamPort, received);
        await using var host = await LoopbackServers.RelayHostAsync(hostPort, upstreamPort);
        using var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri($"ws://127.0.0.1:{hostPort}/relay?team=team-payments"), CancellationToken.None);

        await client.SendAsync("{\"type\":\"session.update\"}"u8.ToArray(), WebSocketMessageType.Text, true, CancellationToken.None);
        await client.SendAsync("{\"type\":\"input_audio_buffer.append\",\"audio\":\"AAAA\"}"u8.ToArray(), WebSocketMessageType.Text, true, CancellationToken.None);
        var reply = await ReadTextAsync(client);

        Assert.Equal("{\"type\":\"input_audio_buffer.speech_started\"}", reply);
        Assert.Equal(["{\"type\":\"session.update\"}", "{\"type\":\"input_audio_buffer.append\",\"audio\":\"AAAA\"}"], received);
    }

    [Fact]
    public async Task Relay_WithNoTeam_ClosesWithPolicyViolationAndNeverOpensUpstream()
    {
        var received = new List<string>();
        var upstreamPort = LoopbackServers.FreePort();
        var hostPort = LoopbackServers.FreePort();
        await using var fake = await LoopbackServers.FakeVoiceLiveAsync(upstreamPort, received);
        await using var host = await LoopbackServers.RelayHostAsync(hostPort, upstreamPort);
        using var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri($"ws://127.0.0.1:{hostPort}/relay"), CancellationToken.None);

        var buffer = new byte[256];
        var result = await client.ReceiveAsync(buffer, CancellationToken.None);

        Assert.Equal(WebSocketMessageType.Close, result.MessageType);
        Assert.Equal(VoiceRelay.TeamRequired, client.CloseStatus);
        Assert.Empty(received);
    }

    private static async Task<string> ReadTextAsync(ClientWebSocket client)
    {
        var buffer = new byte[64 * 1024];
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var result = await client.ReceiveAsync(buffer, cts.Token);
        return System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
    }
}
