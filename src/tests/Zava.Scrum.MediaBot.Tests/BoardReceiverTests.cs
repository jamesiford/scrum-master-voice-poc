using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Zava.Scrum.MediaBot.Board;

namespace Zava.Scrum.MediaBot.Tests;

public sealed class BoardReceiverTests
{
    private static readonly JsonSerializerOptions ReportOptions = new() { WriteIndented = true, NewLine = "\n" };

    [Fact]
    public async Task Share_FirstChangedAndReconnectedFrames_ReachHeadlessTcpReceiverWithinTwoSeconds()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var accept = listener.AcceptTcpClientAsync(timeout.Token);
        using var sender = new TcpClient();
        await sender.ConnectAsync((IPEndPoint)listener.LocalEndpoint, timeout.Token);
        using var receiver = await accept;
        var received = Channel.CreateBounded<ReceivedFrame>(4);
        var receive = ReceiveAsync(receiver.GetStream(), received.Writer, timeout.Token);
        var samples = new List<object>();
        try
        {
            for (var i = 0; i < 10; i++)
            {
                var transport = new TcpTransport(sender.GetStream());
                await using var share = new BoardShareSession(transport, TimeProvider.System, NullLogger.Instance,
                    "team-board-probe", Guid.NewGuid(), (_, _) => { });
                share.Start();
                share.SetEstablished(true);
                var began = Stopwatch.GetTimestamp();
                share.Update(Board($"First {i}"));
                var first = await NextAsync(received.Reader, began, timeout.Token);
                var firstMs = Stopwatch.GetElapsedTime(began, first.At).TotalMilliseconds;
                Assert.Equal(transport.LastHash, first.Hash);

                began = Stopwatch.GetTimestamp();
                share.Update(Board($"Changed {i}"));
                var changed = await NextAsync(received.Reader, began, timeout.Token);
                var changeMs = Stopwatch.GetElapsedTime(began, changed.At).TotalMilliseconds;
                Assert.NotEqual(first.Hash, changed.Hash);
                Assert.Equal(transport.LastHash, changed.Hash);

                began = Stopwatch.GetTimestamp();
                share.Reconnect();
                var reconnected = await NextAsync(received.Reader, began, timeout.Token);
                var reconnectMs = Stopwatch.GetElapsedTime(began, reconnected.At).TotalMilliseconds;
                Assert.Equal(transport.LastHash, reconnected.Hash);
                Assert.Equal(2, transport.Starts);
                Assert.Equal(1, transport.Stops);
                Assert.All(new[] { firstMs, changeMs, reconnectMs }, ms => Assert.InRange(ms, 0, 2000));
                samples.Add(new { firstMs, changeMs, reconnectMs, changedHash = changed.Hash });
            }
        }
        finally
        {
            await timeout.CancelAsync();
            try { await receive; }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested) { }
        }

        var root = FindRoot();
        var output = Path.Combine(root, "docs", "gates", "r2-board");
        Directory.CreateDirectory(output);
        await File.WriteAllTextAsync(Path.Combine(output, "receiver.json"), JsonSerializer.Serialize(new
        {
            measuredUtc = DateTimeOffset.UtcNow,
            boundary = "Actual BoardShareSession -> packed RGB24 over loopback TCP -> independently allocated receiver frame and SHA256",
            thresholdMs = 2000,
            samples,
            sampleSizePerEvent = 10,
            teamsDelivery = new { status = "not_run", n = 0 },
            sdkEncoderCpu = new { status = "not_run", n = 0 },
        }, ReportOptions) + "\n", CancellationToken.None);
    }

    private static JsonElement Board(string goal) => JsonSerializer.SerializeToElement(new
    {
        opened = true,
        ended = false,
        recording = false,
        sprint = "Synthetic receiver fixture",
        goal,
        items = Array.Empty<object>(),
        team = Array.Empty<object>(),
    });

    private static async Task ReceiveAsync(NetworkStream stream, ChannelWriter<ReceivedFrame> writer, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var pixels = new byte[BoardRenderer.Width * BoardRenderer.Height * 3];
            await stream.ReadExactlyAsync(pixels, ct);
            var hash = Convert.ToHexStringLower(SHA256.HashData(pixels));
            await writer.WriteAsync(new ReceivedFrame(Stopwatch.GetTimestamp(), hash), ct);
        }
    }

    private static async Task<ReceivedFrame> NextAsync(ChannelReader<ReceivedFrame> reader, long began, CancellationToken ct)
    {
        while (true)
        {
            var frame = await reader.ReadAsync(ct);
            if (frame.At >= began) { return frame; }
        }
    }

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ZavaScrum.slnx"))) { return directory.FullName; }
        }

        throw new DirectoryNotFoundException("The receiver probe requires the repository checkout.");
    }

    private sealed record ReceivedFrame(long At, string Hash);

    private sealed class TcpTransport(NetworkStream stream) : IBoardShareTransport
    {
        private bool _active;
        public string? LastHash { get; private set; }
        public int Starts { get; private set; }
        public int Stops { get; private set; }

        public Task StartAsync(CancellationToken ct)
        {
            Starts++;
            _active = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct)
        {
            Stops++;
            _active = false;
            return Task.CompletedTask;
        }

        public bool TrySend(byte[] rgb)
        {
            if (!_active) { return false; }
            LastHash = Convert.ToHexStringLower(SHA256.HashData(rgb));
            stream.Write(rgb);
            return true;
        }
    }
}
