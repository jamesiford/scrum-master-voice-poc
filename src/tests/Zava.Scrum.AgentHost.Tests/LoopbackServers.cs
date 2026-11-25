using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zava.Scrum.AgentHost.Voice;

namespace Zava.Scrum.AgentHost.Tests;

/// <summary>
/// Real sockets on loopback: a fake Voice Live that answers audio with the events the real service sends, and the hosted relay in front of it.
/// Only the upstream service is faked; the relay and the client factory under test are the shipped code.
/// </summary>
internal static class LoopbackServers
{
    /// <summary>Starts a fake Voice Live that replies <c>speech_started</c> to every <c>input_audio_buffer.append</c> and records what it received.</summary>
    public static async Task<WebApplication> FakeVoiceLiveAsync(int port, List<string> received)
    {
        var app = Build(port);
        app.UseWebSockets();
        app.Map("/voice-live/realtime", async (HttpContext http) =>
        {
            using var socket = await http.WebSockets.AcceptWebSocketAsync();
            var buffer = new byte[64 * 1024];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                var text = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                lock (received)
                {
                    received.Add(text);
                }

                if (text.Contains("input_audio_buffer.append", StringComparison.Ordinal))
                {
                    await socket.SendAsync("{\"type\":\"input_audio_buffer.speech_started\"}"u8.ToArray(), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        });
        await app.StartAsync();
        return app;
    }

    /// <summary>Starts the relay on <c>/relay</c> in front of the fake Voice Live, exactly as the hosted handler wires it.</summary>
    public static async Task<WebApplication> RelayHostAsync(int port, int upstreamPort)
    {
        var app = Build(port);
        app.UseWebSockets();
        app.Map("/relay", async (HttpContext http) =>
        {
            using var caller = await http.WebSockets.AcceptWebSocketAsync();
            await VoiceRelay.RunAsync(caller, http.Request.Query["team"], async ct =>
            {
                var upstream = new ClientWebSocket();
                await upstream.ConnectAsync(new Uri($"ws://127.0.0.1:{upstreamPort}/voice-live/realtime"), ct);
                return upstream;
            }, http.RequestAborted);
        });
        await app.StartAsync();
        return app;
    }

    public static int FreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static WebApplication Build(int port)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        builder.Logging.ClearProviders();
        builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(1));
        return builder.Build();
    }
}
