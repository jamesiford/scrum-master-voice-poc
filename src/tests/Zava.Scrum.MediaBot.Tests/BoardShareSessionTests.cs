using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace Zava.Scrum.MediaBot.Tests;

public sealed class BoardShareSessionTests
{
    [Fact]
    public async Task Update_OpeningChangeReconnectAndEnd_NegotiatesIndependentShare()
    {
        var transport = new FakeTransport();
        var status = new ConcurrentQueue<string>();
        await using var share = new BoardShareSession(transport, TimeProvider.System, NullLogger.Instance,
            "team-one", Guid.NewGuid(), (state, _) => status.Enqueue(state));
        share.Start();
        share.SetEstablished(true);
        share.Update(Board(opened: false));
        await Task.Delay(150);
        Assert.Equal(0, transport.Starts);

        share.Update(Board(opened: true));
        await WaitAsync(() => transport.Frames.Count == 1);
        Assert.Equal(1, transport.Starts);
        var first = transport.Frames.Single();
        share.Update(Board(opened: true, goal: "Changed goal"));
        await WaitAsync(() => transport.Frames.Any(frame => !frame.SequenceEqual(first)));
        share.Reconnect();
        await WaitAsync(() => transport.Starts == 2);
        share.Update(Board(opened: true, ended: true));
        await WaitAsync(() => transport.Stops == 2);
        var frames = transport.Frames.Count;
        await Task.Delay(200);
        Assert.Equal(frames, transport.Frames.Count);
        Assert.Contains("Stopped", status);
    }

    [Fact]
    public async Task Update_FailedShare_ReportsFailureWithoutFaultingOwner()
    {
        var transport = new FakeTransport { RefuseStart = true };
        var status = new ConcurrentQueue<string>();
        await using var share = new BoardShareSession(transport, TimeProvider.System, NullLogger.Instance,
            "team-one", Guid.NewGuid(), (state, _) => status.Enqueue(state));
        share.Start();
        share.SetEstablished(true);
        share.Update(Board(opened: true));
        await WaitAsync(() => status.Contains("Failed"));
        Assert.Empty(transport.Frames);
    }

    [Fact]
    public async Task Update_UnchangedBoard_ResendsCachedPixelsAtLowRate()
    {
        var transport = new FakeTransport();
        await using var share = new BoardShareSession(transport, TimeProvider.System, NullLogger.Instance,
            "team-one", Guid.NewGuid(), (_, _) => { });
        share.Start();
        share.SetEstablished(true);
        share.Update(Board(opened: true));
        await WaitAsync(() => transport.Frames.Count >= 2);
        Assert.InRange(transport.Frames.Count, 2, 3);
        Assert.All(transport.Frames, frame => Assert.Same(transport.Frames.First(), frame));
    }

    private static JsonElement Board(bool opened, bool ended = false, string goal = "Ship") =>
        JsonSerializer.SerializeToElement(new
        {
            type = "board",
            opened,
            ended,
            sprint = "Sprint",
            goal,
            daysLeft = 5,
            speaker = (string?)null,
            focus = (int?)null,
            items = Array.Empty<object>(),
            team = Array.Empty<object>(),
        });

    private static async Task WaitAsync(Func<bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!predicate()) { await Task.Delay(10, timeout.Token); }
    }

    private sealed class FakeTransport : IBoardShareTransport
    {
        private int _starts;
        private int _stops;

        public bool Active => Starts > Stops;

        public bool RefuseStart { get; init; }

        public int Starts => Volatile.Read(ref _starts);

        public int Stops => Volatile.Read(ref _stops);

        public ConcurrentQueue<byte[]> Frames { get; } = new();

        public Task StartAsync(CancellationToken ct)
        {
            if (RefuseStart) { throw new InvalidOperationException("Sharing refused"); }
            Interlocked.Increment(ref _starts);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct)
        {
            Interlocked.Increment(ref _stops);
            return Task.CompletedTask;
        }

        public void Send(byte[] rgb) => Frames.Enqueue(rgb);
    }
}
