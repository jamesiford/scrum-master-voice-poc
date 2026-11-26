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

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Reconnect_FailedStartOrSend_ReestablishesIndependentShare(bool failWhileSending, bool timeout)
    {
        var transport = new FakeTransport
        {
            StartFailures = failWhileSending ? 0 : 1,
            SendFailures = failWhileSending ? 1 : 0,
            SendTimeout = timeout,
        };
        var status = new ConcurrentQueue<string>();
        await using var share = new BoardShareSession(transport, TimeProvider.System, NullLogger.Instance,
            "team-one", Guid.NewGuid(), (state, _) => status.Enqueue(state));
        share.Start();
        share.SetEstablished(true);
        share.Update(Board(opened: true));
        await WaitAsync(() => status.Contains("Failed"));
        await Task.Delay(300);
        Assert.Equal(1, transport.StartAttempts);

        share.Reconnect();
        await WaitAsync(() => !transport.Frames.IsEmpty && status.Contains("Sharing"));
        Assert.Equal(failWhileSending ? 2 : 1, transport.Starts);
        share.Update(Board(opened: true, ended: true));
        await WaitAsync(() => transport.Stops == 2);
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

    [Fact]
    public async Task Update_SocketNotReady_ReportsSharingOnlyAfterFrameAccepted()
    {
        var transport = new FakeTransport { AcceptFrames = false };
        var status = new ConcurrentQueue<string>();
        await using var share = new BoardShareSession(transport, TimeProvider.System, NullLogger.Instance,
            "team-one", Guid.NewGuid(), (state, _) => status.Enqueue(state));
        share.Start();
        share.SetEstablished(true);
        share.Update(Board(opened: true));
        await WaitAsync(() => transport.SendAttempts != 0);
        Assert.Empty(transport.Frames);
        Assert.DoesNotContain("Sharing", status);

        transport.AcceptFrames = true;
        await WaitAsync(() => !transport.Frames.IsEmpty && status.Contains("Sharing"));
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
        private int _startAttempts;
        private int _sendAttempts;
        private int _active;
        private int _acceptFrames = 1;

        public bool Active => Volatile.Read(ref _active) != 0;

        public bool RefuseStart { get; init; }

        public int StartFailures { get; init; }

        public int SendFailures { get; init; }

        public bool SendTimeout { get; init; }

        public bool AcceptFrames
        {
            get => Volatile.Read(ref _acceptFrames) != 0;
            set => Volatile.Write(ref _acceptFrames, value ? 1 : 0);
        }

        public int SendAttempts => Volatile.Read(ref _sendAttempts);

        public int StartAttempts => Volatile.Read(ref _startAttempts);

        public int Starts => Volatile.Read(ref _starts);

        public int Stops => Volatile.Read(ref _stops);

        public ConcurrentQueue<byte[]> Frames { get; } = new();

        public Task StartAsync(CancellationToken ct)
        {
            var attempt = Interlocked.Increment(ref _startAttempts);
            if (RefuseStart || attempt <= StartFailures) { throw new InvalidOperationException("Sharing refused"); }
            Interlocked.Increment(ref _starts);
            Volatile.Write(ref _active, 1);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct)
        {
            Interlocked.Increment(ref _stops);
            Volatile.Write(ref _active, 0);
            return Task.CompletedTask;
        }

        public bool TrySend(byte[] rgb)
        {
            var attempt = Interlocked.Increment(ref _sendAttempts);
            if (!Active || !AcceptFrames) { return false; }
            if (attempt <= SendFailures)
            {
                throw SendTimeout ? new OperationCanceledException("Send timed out") : new InvalidOperationException("Send failed");
            }
            Frames.Enqueue(rgb);
            return true;
        }
    }
}
