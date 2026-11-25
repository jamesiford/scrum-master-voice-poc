using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Infrastructure.Agents;
using Zava.Scrum.Infrastructure.Voice;

// S6 and G1 measurement (ADR 0015 S6, ADR 0021): the same Voice Live session opened directly and through the hosted relay, alternating in the
// same time window so the paired difference, not two medians, is reported.
//   dotnet run --project tools/VoiceRelayBench -- --mode paired --direct wss://<acct>.services.ai.azure.com/voice-live/realtime
//       --relay wss://<acct>.services.ai.azure.com/api/projects/<p>/agents/<agent>/endpoint/protocols/invocations_ws
//       --speech https://<acct>.cognitiveservices.azure.com --tenant <id> [--alternations 4] [--turns 8] [--out file.json]
// Modes: paired (turn latency), bargein, connect (new-session and reconnect times), idle (--idle-minutes N).
var arguments = args.ToList();
string? Arg(string name) => arguments.IndexOf(name) is var i and >= 0 && i + 1 < arguments.Count ? arguments[i + 1] : null;
var mode = Arg("--mode") ?? "paired";
var directEndpoint = Arg("--direct") ?? throw new ArgumentException("--direct is required");
var relayEndpoint = Arg("--relay") ?? throw new ArgumentException("--relay is required");
var speechEndpoint = Arg("--speech") ?? throw new ArgumentException("--speech is required");
var alternations = int.Parse(Arg("--alternations") ?? "4", System.Globalization.CultureInfo.InvariantCulture);
var turns = int.Parse(Arg("--turns") ?? "8", System.Globalization.CultureInfo.InvariantCulture);
var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = Arg("--tenant") });

IVoiceSessionFactory Factory(bool relay, string sessionId = "") => new VoiceLiveSessionFactory(
    credential,
    Options.Create(new VoiceLiveOptions { Endpoint = directEndpoint, RelayEndpoint = relay ? relayEndpoint : string.Empty, RelaySessionId = sessionId }),
    Options.Create(new FoundryOptions { ProjectEndpoint = "https://" + new Uri(directEndpoint.Replace("wss://", "https://", StringComparison.Ordinal)).Host + "/api/projects/p" }));
var config = new VoiceSessionConfig("You are a brief, friendly scrum master. Answer in one short sentence.", [], 800, "team-bench");

string[] questions =
[
    "What is blocking the payments retry work today?",
    "Which of my items are overdue this sprint?",
    "Can you remind me what I said I would finish yesterday?",
    "Is the checkout ticket still assigned to Daniel?",
    "How many story points are left in the sprint?",
    "Who is working on the webhook signing item?",
    "What was the due date for the refund endpoint?",
    "Do we have any open blockers for the release?",
];
using var http = new HttpClient();
async Task<byte[]> Speak(string text)
{
    var token = await credential.GetTokenAsync(new TokenRequestContext(["https://cognitiveservices.azure.com/.default"]), CancellationToken.None);
    using var request = new HttpRequestMessage(HttpMethod.Post, speechEndpoint + "/tts/cognitiveservices/v1")
    {
        Content = new StringContent($"<speak version=\"1.0\" xml:lang=\"en-US\"><voice name=\"en-US-AndrewNeural\">{System.Security.SecurityElement.Escape(text)}</voice></speak>", Encoding.UTF8, "application/ssml+xml"),
    };
    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/ssml+xml");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
    request.Headers.UserAgent.ParseAdd("zava-voice-relay-bench");
    request.Headers.Add("X-Microsoft-OutputFormat", "raw-24khz-16bit-mono-pcm");
    using var response = await http.SendAsync(request);
    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException($"Speech synthesis failed {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    return await response.Content.ReadAsByteArrayAsync();
}

var audio = new List<byte[]>();
foreach (var q in questions)
{
    audio.Add(await Speak(q));
}

Console.Error.WriteLine($"synthesised {audio.Count} questions, {audio.Sum(a => a.Length) / 48000.0:F1} s of audio");
var result = new Dictionary<string, object>();

switch (mode)
{
    case "paired":
        result["paired"] = await Paired();
        break;
    case "bargein":
        result["bargein"] = await BargeIn();
        break;
    case "connect":
        result["connect"] = await Connect();
        break;
    case "idle":
        result["idle"] = await Idle(int.Parse(Arg("--idle-minutes") ?? "16", System.Globalization.CultureInfo.InvariantCulture));
        break;
    default:
        throw new ArgumentException("unknown --mode " + mode);
}

var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(json);
if (Arg("--out") is { } outFile)
{
    await File.WriteAllTextAsync(outFile, json);
}

return 0;

async Task<object> Paired()
{
    var rows = new List<Turn>();
    var connect = new List<(string Path, long Ms)>();
    for (var a = 0; a < alternations; a++)
    {
        // Alternate which path goes first so neither always runs first inside a window.
        foreach (var relay in a % 2 == 0 ? new[] { false, true } : new[] { true, false })
        {
            var path = relay ? "relay" : "direct";
            var clock = Stopwatch.StartNew();
            await using var session = await Factory(relay).CreateAsync(config, CancellationToken.None);
            connect.Add((path, clock.ElapsedMilliseconds));
            using var channel = new EventChannel(session);
            for (var t = 0; t < turns; t++)
            {
                rows.Add(await RunTurn(session, channel, audio[t % audio.Count], path, a, t));
            }
        }
    }

    var ok = rows.Where(r => r.Failed is null).ToList();
    var pairs = ok.Where(r => r.Path == "direct").Join(ok.Where(r => r.Path == "relay"), d => (d.Block, d.Index), r => (r.Block, r.Index), (d, r) => (d, r)).ToList();
    return new
    {
        turnsDirect = ok.Count(r => r.Path == "direct"),
        turnsRelay = ok.Count(r => r.Path == "relay"),
        failed = rows.Count(r => r.Failed is not null),
        failures = rows.Where(r => r.Failed is not null).Select(r => $"{r.Path} {r.Block}/{r.Index}: {r.Failed}").ToList(),
        direct = Summary(ok.Where(r => r.Path == "direct")),
        relay = Summary(ok.Where(r => r.Path == "relay")),
        pairedDifferenceMs = new
        {
            pairs = pairs.Count,
            speechStarted = Stats(pairs.Select(p => p.r.SpeechStartedMs - p.d.SpeechStartedMs)),
            speechStopped = Stats(pairs.Select(p => p.r.StopMs - p.d.StopMs)),
            firstAudioFromResponseCreate = Stats(pairs.Select(p => p.r.FirstAudioMs - p.d.FirstAudioMs)),
            totalSpeechEndToFirstAudio = Stats(pairs.Select(p => p.r.TotalMs - p.d.TotalMs)),
        },
        connectMs = connect.GroupBy(c => c.Path).ToDictionary(g => g.Key, g => Stats(g.Select(c => (double)c.Ms))),
        blockMedianTotalMs = rows.GroupBy(r => (r.Block, r.Path)).OrderBy(g => g.Key.Block).Select(g => $"{g.Key.Block} {g.Key.Path} {Stats(g.Where(r => r.Failed is null).Select(r => r.TotalMs)).P50}").ToList(),
    };
}

async Task<Turn> RunTurn(IVoiceSession session, EventChannel channel, byte[] pcm, string path, int block, int index)
{
    channel.Drain();
    const int frameBytes = 960; // 20 ms of PCM16 at 24 kHz mono, as on the bridge
    var turn = new Turn { Path = path, Block = block, Index = index };
    var speechFrames = (pcm.Length + frameBytes - 1) / frameBytes;
    var start = channel.Now;
    double speechEnd = 0;
    double? started = null;
    double? stopped = null;
    for (var frame = 0; frame < speechFrames + 300 && stopped is null; frame++)
    {
        var wait = start + (frame * 20.0) - channel.Now;
        if (wait > 1)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(wait));
        }

        var chunk = frame < speechFrames ? pcm.AsMemory(frame * frameBytes, Math.Min(frameBytes, pcm.Length - (frame * frameBytes))) : new byte[frameBytes];
        await session.SendAudioAsync(chunk, CancellationToken.None);
        if (frame == speechFrames - 1)
        {
            speechEnd = channel.Now;
        }

        while (channel.TryRead(out var seen))
        {
            started ??= seen.Event is UserSpeechStarted ? seen.At : null;
            stopped ??= seen.Event is UserSpeechStopped ? seen.At : null;
        }
    }

    if (started is null || stopped is null)
    {
        turn.Failed = "no speech events";
        return turn;
    }

    turn.SpeechStartedMs = started.Value - start;
    turn.StopMs = stopped.Value - speechEnd;
    var created = channel.Now;
    await session.CreateResponseAsync("Answer the question in one short sentence.", CancellationToken.None);
    double? firstAudio = null;
    while (await channel.ReadAsync(TimeSpan.FromSeconds(25)) is { } e)
    {
        if (e.Event is AgentAudio && firstAudio is null)
        {
            firstAudio = e.At;
        }

        if (e.Event is VoiceFault fault)
        {
            turn.Failed = fault.Message;
            return turn;
        }

        if (e.Event is ResponseDone)
        {
            break;
        }
    }

    if (firstAudio is null)
    {
        turn.Failed = "no audio";
        return turn;
    }

    turn.FirstAudioMs = firstAudio.Value - created;
    turn.TotalMs = firstAudio.Value - speechEnd;
    return turn;
}

async Task<object> BargeIn()
{
    var rows = new List<(string Path, double Ms)>();
    for (var a = 0; a < alternations; a++)
    {
        foreach (var relay in a % 2 == 0 ? new[] { false, true } : new[] { true, false })
        {
            await using var session = await Factory(relay).CreateAsync(config, CancellationToken.None);
            using var channel = new EventChannel(session);
            for (var i = 0; i < 3; i++)
            {
                channel.Drain();
                await session.CreateResponseAsync("Count slowly from one to thirty, saying each number as a separate sentence.", CancellationToken.None);
                (VoiceEvent Event, double At)? e;
                do
                {
                    e = await channel.ReadAsync(TimeSpan.FromSeconds(20));
                }
                while (e is not null && e.Value.Event is not AgentAudio);

                await Task.Delay(1500);
                var cancelled = channel.Now;
                await session.CancelResponseAsync(CancellationToken.None);
                var last = cancelled;
                while (await channel.ReadAsync(TimeSpan.FromMilliseconds(700)) is { } seen)
                {
                    if (seen.Event is AgentAudio)
                    {
                        last = seen.At;
                    }
                }

                rows.Add((relay ? "relay" : "direct", last - cancelled));
            }
        }
    }

    return new
    {
        description = "ms from sending response.cancel to the last audio frame still received (audio already in flight)",
        direct = Stats(rows.Where(r => r.Path == "direct").Select(r => r.Ms)),
        relay = Stats(rows.Where(r => r.Path == "relay").Select(r => r.Ms)),
    };
}

async Task<object> Connect()
{
    var rows = new List<(string Path, string Kind, long Ms)>();
    for (var a = 0; a < alternations; a++)
    {
        foreach (var relay in a % 2 == 0 ? new[] { false, true } : new[] { true, false })
        {
            var path = relay ? "relay" : "direct";
            var id = Guid.NewGuid().ToString();
            var clock = Stopwatch.StartNew();
            await using (var first = await Factory(relay, id).CreateAsync(config, CancellationToken.None))
            {
                rows.Add((path, "new session", clock.ElapsedMilliseconds));
            }

            clock.Restart();
            await using var again = await Factory(relay, id).CreateAsync(config, CancellationToken.None);
            rows.Add((path, "reconnect same session", clock.ElapsedMilliseconds));
        }
    }

    return rows.GroupBy(r => (r.Path, r.Kind)).ToDictionary(g => $"{g.Key.Path} {g.Key.Kind}", g => Stats(g.Select(r => (double)r.Ms)));
}

async Task<object> Idle(int minutes)
{
    var id = Guid.NewGuid().ToString();
    var clock = Stopwatch.StartNew();
    await using (var first = await Factory(true, id).CreateAsync(config, CancellationToken.None))
    {
        Console.Error.WriteLine($"first connect {clock.ElapsedMilliseconds} ms; idling {minutes} minutes");
    }

    await Task.Delay(TimeSpan.FromMinutes(minutes));
    clock.Restart();
    await using var again = await Factory(true, id).CreateAsync(config, CancellationToken.None);
    var reconnect = clock.ElapsedMilliseconds;
    using var channel = new EventChannel(again);
    var turn = await RunTurn(again, channel, audio[0], "relay", 0, 0);
    return new { idleMinutes = minutes, reconnectAfterIdleMs = reconnect, turnAfterIdleFailed = turn.Failed, turnTotalMs = turn.TotalMs };
}

static object Summary(IEnumerable<Turn> turns)
{
    var list = turns.ToList();
    return new
    {
        speechStartedMs = Stats(list.Select(t => t.SpeechStartedMs)),
        speechStoppedMs = Stats(list.Select(t => t.StopMs)),
        firstAudioFromResponseCreateMs = Stats(list.Select(t => t.FirstAudioMs)),
        totalSpeechEndToFirstAudioMs = Stats(list.Select(t => t.TotalMs)),
    };
}

static St Stats(IEnumerable<double> values)
{
    var v = values.Order().ToList();
    if (v.Count == 0)
    {
        return new St(0, 0, 0, 0);
    }

    double At(double q) => v[Math.Min(v.Count - 1, (int)Math.Ceiling((q * v.Count) - 1))];
    return new St(Math.Round(At(0.5), 1), Math.Round(At(0.95), 1), Math.Round(v.Average(), 1), v.Count);
}

internal sealed record St(double P50, double P95, double Mean, int N);

internal sealed class Turn
{
    public string Path { get; set; } = string.Empty;

    public int Block { get; set; }

    public int Index { get; set; }

    public double SpeechStartedMs { get; set; }

    public double StopMs { get; set; }

    public double FirstAudioMs { get; set; }

    public double TotalMs { get; set; }

    public string? Failed { get; set; }
}

/// <summary>Reads a voice session's events into a queue with arrival times on one clock, so the measuring loop never blocks the reader.</summary>
internal sealed class EventChannel : IDisposable
{
    private readonly Channel<(VoiceEvent Event, double At)> _queue = Channel.CreateUnbounded<(VoiceEvent Event, double At)>();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly CancellationTokenSource _stop = new();

    public EventChannel(IVoiceSession session) => _ = Task.Run(async () =>
    {
        try
        {
            await foreach (var e in session.ReadEventsAsync(_stop.Token))
            {
                _queue.Writer.TryWrite((e, _clock.Elapsed.TotalMilliseconds));
            }
        }
        catch (OperationCanceledException)
        {
            // Disposed.
        }
    });

    public double Now => _clock.Elapsed.TotalMilliseconds;

    public void Drain()
    {
        while (_queue.Reader.TryRead(out _))
        {
        }
    }

    public bool TryRead(out (VoiceEvent Event, double At) item) => _queue.Reader.TryRead(out item);

    public async Task<(VoiceEvent Event, double At)?> ReadAsync(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout > TimeSpan.Zero ? timeout : TimeSpan.FromMilliseconds(1));
        try
        {
            return await _queue.Reader.ReadAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    public void Dispose() => _stop.Cancel();
}
