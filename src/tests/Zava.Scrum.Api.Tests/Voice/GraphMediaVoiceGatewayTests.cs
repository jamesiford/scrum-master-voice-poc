using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Zava.Scrum.Api.Voice;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Standups;
using Zava.Scrum.Domain.Kpis;
using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Standups;
using Xunit.Abstractions;

namespace Zava.Scrum.Api.Tests.Voice;

public sealed class GraphMediaVoiceGatewayTests(ITestOutputHelper output)
{
    [Fact]
    public async Task RunAsync_ThreeSyntheticIdentityStreams_AttributesEverySelectedFrame()
    {
        var identities = new[] { Fixture.ObjectId, Guid.NewGuid(), Guid.NewGuid() };
        await using var fixture = new Fixture(identities);
        await fixture.ReadyAsync();
        fixture.Socket.Input(new
        {
            type = "participants",
            items = identities.Select((id, i) => new { participantId = $"speaker-{i}", objectId = id, displayName = $"Voice {i}" }).ToArray(),
        });
        await WaitAsync(() => fixture.Live.GetState(fixture.Context.Session.Id)?.Contains("\"memberId\":\"admin\"", StringComparison.Ordinal) == true);

        for (var i = 0; i < 90; i++)
        {
            fixture.Audio(identities[i % 3], marker: (byte)(i % 3 + 1));
        }

        await WaitAsync(() => fixture.Voice.Audio.Count(b => b.Any(value => value != 0)) == 90);
        var frames = fixture.Voice.Audio.Where(b => b.Any(value => value != 0)).ToArray();
        Assert.Equal(Enumerable.Range(0, 90).Select(i => (byte)(i % 3 + 1)), frames.Select(b => b[0]));
        foreach (var marker in new byte[] { 1, 2, 3 })
        {
            Assert.Equal(30, frames.Count(b => b[0] == marker));
        }

        output.WriteLine("Synthetic identity-tagged PCM: 90/90 selected frames, 30 per roster identity. This is not SDK/Teams attribution or voice recognition.");
    }

    [Fact]
    public async Task RunAsync_OverlappingGuestIsLouder_DoesNotForwardQuieterMemberAudio()
    {
        await using var fixture = new Fixture();
        await fixture.ReadyAsync();
        fixture.Socket.Input(new
        {
            type = "audio",
            items = new[]
            {
                new { participantId = "member", participantObjectId = Fixture.ObjectId, displayName = "Daniel", amplitude = 500, pcm = Convert.ToBase64String(Enumerable.Repeat((byte)1, 960).ToArray()) },
                new { participantId = "guest", participantObjectId = Guid.NewGuid(), displayName = "Daniel", amplitude = 1000, pcm = Convert.ToBase64String(Enumerable.Repeat((byte)2, 960).ToArray()) },
            },
        });
        fixture.Audio(Fixture.ObjectId, marker: 3);
        await WaitAsync(() => fixture.Voice.Audio.Any(b => b[0] == 3));

        Assert.DoesNotContain(fixture.Voice.Audio, b => b[0] is 1 or 2);
        output.WriteLine("Overlap probe: strongest guest suppresses the simultaneous quieter member frame; only subsequent member PCM is forwarded.");
    }

    [Fact]
    public async Task RunAsync_SustainedSpeechDuringOpening_RetainsSharedFloorException()
    {
        await using var fixture = new Fixture();
        await fixture.ReadyAsync();
        fixture.Socket.Input(new { type = "callState", state = "Established" });
        await WaitAsync(() => fixture.Voice.Responses.Count == 1);
        fixture.Voice.Emit(new AgentResponseStarted());
        fixture.Audio(Fixture.ObjectId);
        fixture.Voice.Emit(new UserSpeechStarted());
        await Task.Delay(50);
        fixture.Clock.Advance(TimeSpan.FromMilliseconds(600));
        await Task.Delay(100);

        Assert.Equal(0, fixture.Voice.Cancels);
        Assert.False(fixture.Socket.HasType("stopAudio"));
        output.WriteLine("Opening exception: sustained 600 ms synthetic speech does not cancel. Unconditional barge-in requirement is not satisfied in this state.");
    }

    [Fact]
    public async Task RunAsync_SameNameGuest_ReceivesNoMemberAudioOrWork()
    {
        await using var fixture = new Fixture();
        await fixture.ReadyAsync();
        fixture.Socket.Input(new
        {
            type = "participants",
            items = new[] { new { participantId = "guest", objectId = Guid.NewGuid(), displayName = "Daniel" } },
        });
        fixture.Audio(Guid.NewGuid());
        await WaitAsync(() => fixture.Live.GetState(fixture.Context.Session.Id)?.Contains("\"guest\":true", StringComparison.Ordinal) == true);

        Assert.DoesNotContain(fixture.Voice.Audio, bytes => bytes.Any(b => b != 0));
        Assert.Empty(fixture.Voice.Responses);
    }

    [Fact]
    public async Task RunAsync_MalformedProducerMessage_ReleasesTheSessionClaim()
    {
        await using var fixture = new Fixture();
        await fixture.ReadyAsync();
        fixture.Socket.BrokenJson();

        await Assert.ThrowsAnyAsync<JsonException>(() => fixture.Completion);
        fixture.FaultObserved = true;

        Assert.Null(fixture.Live.GetState(fixture.Context.Session.Id));
    }

    [Fact]
    public async Task RunAsync_GuestCrossTalk_HoldsSilenceWithoutSendingGuestAudio()
    {
        await using var fixture = new Fixture();
        await fixture.ReadyAsync();
        fixture.Socket.Input(new { type = "callState", state = "Established" });
        await WaitAsync(() => fixture.Voice.Responses.Count == 1);
        fixture.Voice.Emit(new AgentResponseStarted());
        fixture.Socket.Input(new { type = "agentAudioState", state = "idle", playedMs = 20 });
        await WaitAsync(() => fixture.Voice.Responses.Count == 2);
        fixture.Voice.Emit(new AgentResponseStarted());
        fixture.Socket.Input(new { type = "agentAudioState", state = "idle", playedMs = 20 });
        fixture.Audio(Fixture.ObjectId);
        await WaitAsync(() => fixture.Voice.Audio.Any(b => b.Any(value => value != 0)));
        fixture.Voice.Emit(new UserSpeechStarted());
        fixture.Voice.Emit(new UserSpeechStopped());
        await Task.Delay(50);
        var guestId = Guid.NewGuid();
        for (var i = 0; i < 55; i++)
        {
            fixture.Clock.Advance(TimeSpan.FromMilliseconds(20));
            fixture.Audio(guestId);
            await Task.Delay(20);
        }

        Assert.Equal(2, fixture.Voice.Responses.Count);
        Assert.Single(fixture.Voice.Audio, b => b.Any(value => value != 0));
    }

    [Fact]
    public async Task RunAsync_SustainedSpeech_CancelsDropsAudioAndTruncatesAtPlayedPosition()
    {
        await using var fixture = new Fixture();
        await fixture.ReadyAsync();
        fixture.Socket.Input(new { type = "callState", state = "Established" });
        await WaitAsync(() => fixture.Voice.Responses.Count == 1);
        fixture.Voice.Emit(new AgentResponseStarted());
        fixture.Socket.Input(new { type = "agentAudioState", state = "idle", playedMs = 20 });
        await WaitAsync(() => fixture.Voice.Responses.Count == 2);
        fixture.Voice.Emit(new AgentResponseStarted());
        fixture.Voice.Emit(new AgentItemStarted("interrupted-item"));
        fixture.Voice.Emit(new AgentAudio(new byte[] { 1, 1 }));
        await WaitAsync(() => fixture.Socket.Sent.Any(s => s.Type == WebSocketMessageType.Binary));
        fixture.Audio(Fixture.ObjectId);
        fixture.Voice.Emit(new UserSpeechStarted());
        await Task.Delay(50);
        var stopLatency = Stopwatch.StartNew();
        fixture.Clock.Advance(TimeSpan.FromMilliseconds(400));
        await WaitAsync(() => fixture.Socket.HasType("stopAudio"));
        stopLatency.Stop();
        await WaitAsync(() => fixture.Voice.Cancels == 1);
        fixture.Voice.Emit(new AgentAudio(new byte[] { 9, 9 }));
        fixture.Socket.Input(new { type = "agentAudioState", state = "idle", interrupted = true, playedMs = 120 });
        await WaitAsync(() => fixture.Voice.Truncations.Count == 1);

        Assert.Equal(("interrupted-item", 120), fixture.Voice.Truncations.Single());
        Assert.DoesNotContain(fixture.Socket.Sent, s => s.Type == WebSocketMessageType.Binary && s.Bytes.SequenceEqual(new byte[] { 9, 9 }));
        output.WriteLine($"Synthetic CallingOn: stop command observed {stopLatency.Elapsed.TotalMilliseconds:F1} ms after clock crossed 350 ms threshold; cancelled once and truncated at 120 ms. Not a Teams playback measurement.");
    }

    private static async Task WaitAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public static readonly Guid ObjectId = Guid.NewGuid();
        private readonly CancellationTokenSource _stop = new();
        private readonly BridgeConnection _bridge;
        private readonly Task<bool> _run;

        public Fixture(Guid[]? identities = null)
        {
            identities ??= [ObjectId];
            var ids = new[] { "daniel", "maya", "admin" };
            var roster = identities.Select((id, i) => new Member(ids[i], "team-one", ids[i], ids[i], null, "", i + 1, true, EntraObjectId: id)).ToArray();
            var brief = new PreBrief("team-one", "sprint", "goal", 5, new Burndown(1, 1, 0, 5, false),
                roster.Select(m => new MemberBrief(m.Id, m.DisplayName, [], "Progress?")).ToArray(), [], [], "summary");
            var session = new SessionDto(Guid.NewGuid(), "team-one", StandupStatus.Live, Clock.UtcNow, brief, "acs", Clock.UtcNow, null, [],
                new Dictionary<string, Attendance>(), 0, 0, 0, 15);
            Context = new MediaSessionContext(session,
                roster, "Instructions", "https://teams.microsoft.com/meet/123");
            _bridge = new BridgeConnection(Socket);
            var gateway = new GraphMediaVoiceGateway(Voice, Clock, Live, Options.Create(new VoiceGatewayOptions { BargeInMs = 350 }),
                NullLogger<GraphMediaVoiceGateway>.Instance);
            _run = gateway.RunAsync(_bridge, Context, _stop.Token);
        }

        public MediaSessionContext Context { get; }

        public FakeSocket Socket { get; } = new();

        public FakeVoice Voice { get; } = new();

        public FakeClock Clock { get; } = new();

        public LiveSessionRegistry Live { get; } = new();

        public Task<bool> Completion => _run;

        public bool FaultObserved { get; set; }

        public Task ReadyAsync() => WaitAsync(() => Socket.HasType("hello"));

        public void Audio(Guid id, byte marker = 1) => Socket.Input(new
        {
            type = "audio",
            items = new[] { new { participantId = "speaker", participantObjectId = id, displayName = "Daniel", amplitude = 500, pcm = Convert.ToBase64String(Enumerable.Repeat(marker, 960).ToArray()) } },
        });

        public async ValueTask DisposeAsync()
        {
            await _stop.CancelAsync();
            if (!FaultObserved) { await _run; }
            _bridge.Dispose();
            Socket.Dispose();
            _stop.Dispose();
        }
    }

    private sealed class FakeClock : IClock
    {
        private long _ticks = DateTimeOffset.UnixEpoch.Ticks;

        public DateTimeOffset UtcNow => new(Interlocked.Read(ref _ticks), TimeSpan.Zero);

        public void Advance(TimeSpan time) => Interlocked.Add(ref _ticks, time.Ticks);
    }

    private sealed class FakeVoice : IVoiceSessionFactory, IVoiceSession
    {
        private readonly Channel<VoiceEvent> _events = Channel.CreateUnbounded<VoiceEvent>();
        private int _cancels;

        public ConcurrentQueue<byte[]> Audio { get; } = new();

        public ConcurrentQueue<string?> Responses { get; } = new();

        public ConcurrentQueue<(string, int)> Truncations { get; } = new();

        public int Cancels => Volatile.Read(ref _cancels);

        public void Emit(VoiceEvent ev) => _events.Writer.TryWrite(ev);

        public Task<IVoiceSession> CreateAsync(VoiceSessionConfig config, CancellationToken ct)
        {
            Assert.Empty(config.Tools);
            return Task.FromResult<IVoiceSession>(this);
        }

        public async IAsyncEnumerable<VoiceEvent> ReadEventsAsync([EnumeratorCancellation] CancellationToken ct)
        {
            await foreach (var ev in _events.Reader.ReadAllAsync(ct)) { yield return ev; }
        }

        public ValueTask SendAudioAsync(ReadOnlyMemory<byte> pcm, CancellationToken ct) { Audio.Enqueue(pcm.ToArray()); return ValueTask.CompletedTask; }

        public Task CreateResponseAsync(string? instructions, CancellationToken ct) { Responses.Enqueue(instructions); return Task.CompletedTask; }

        public Task CancelResponseAsync(CancellationToken ct) { Interlocked.Increment(ref _cancels); return Task.CompletedTask; }

        public Task TruncateAsync(string itemId, int audioEndMs, CancellationToken ct) { Truncations.Enqueue((itemId, audioEndMs)); return Task.CompletedTask; }

        public Task AddUserTextAsync(string text, CancellationToken ct) => throw new NotSupportedException();

        public Task SubmitToolOutputAsync(string callId, string output, CancellationToken ct) => throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeSocket : WebSocket
    {
        private readonly Channel<byte[]> _input = Channel.CreateUnbounded<byte[]>();

        public ConcurrentQueue<(WebSocketMessageType Type, byte[] Bytes)> Sent { get; } = new();

        public void Input(object message) => _input.Writer.TryWrite(JsonSerializer.SerializeToUtf8Bytes(message));

        public void BrokenJson() => _input.Writer.TryWrite([(byte)'{']);

        public bool HasType(string type) => Sent.Any(s => s.Type == WebSocketMessageType.Text
            && JsonDocument.Parse(s.Bytes).RootElement.GetProperty("type").GetString() == type);

        public override WebSocketCloseStatus? CloseStatus => null;

        public override string? CloseStatusDescription => null;

        public override WebSocketState State => WebSocketState.Open;

        public override string? SubProtocol => null;

        public override void Abort() { }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) => Task.CompletedTask;

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) => Task.CompletedTask;

        public override void Dispose() { }

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            var bytes = await _input.Reader.ReadAsync(cancellationToken);
            bytes.AsSpan().CopyTo(buffer.AsSpan());
            return new WebSocketReceiveResult(bytes.Length, WebSocketMessageType.Text, true);
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            Sent.Enqueue((messageType, buffer.ToArray()));
            return Task.CompletedTask;
        }
    }
}
