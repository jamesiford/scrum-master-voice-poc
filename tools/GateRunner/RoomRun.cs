using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Playwright;

namespace Zava.Scrum.Tools.GateRunner;

/// <summary>Drives one Simulated Room standup through the real API and reads back the per-turn timings the voice gateway recorded.</summary>
/// <param name="api">The API base URL.</param>
/// <param name="team">The team id.</param>
/// <param name="token">A bearer token for an API behind Easy Auth, or <see langword="null"/>.</param>
internal sealed class RoomRun(string api, string team, string? token) : IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http = NewClient(api, token);

    /// <summary>Plays the cast (autopilot) from start to the closing recap.</summary>
    /// <param name="label">The configuration label for the result.</param>
    /// <param name="build">The build id.</param>
    /// <param name="timeout">How long to wait for the standup to close.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="retrospective">Runs the existing retrospective cast and records its circle-back proof instead of a standup.</param>
    /// <returns>The run result.</returns>
    public async Task<RunResult> RunAsync(string label, string build, TimeSpan timeout, CancellationToken ct, bool retrospective = false)
    {
        var startedAt = DateTimeOffset.UtcNow;
        await PostAsync($"/api/teams/{team}/members/sync", new { }, ct);
        var sessionId = (await PostAsync(retrospective ? "/api/retrospective/prepare" : "/api/sessions/prepare", new { teamId = team }, ct)).Trim('"');
        var start = JsonSerializer.Deserialize<StartReply>(await PostAsync($"/api/sessions/{sessionId}/start", new { mode = "simulated", standupMinutes = 15 }, ct), Json)
            ?? throw new InvalidOperationException("start returned nothing");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Channel = "msedge", Args = ["--autoplay-policy=no-user-gesture-required"] });
        // Easy Auth on the bridge host accepts the same Entra bearer token as the API.
        var context = await browser.NewContextAsync(new BrowserNewContextOptions { ExtraHTTPHeaders = string.IsNullOrEmpty(token) ? null : new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" } });
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{start.RoomUrl}&autopilot=1");
        await page.WaitForFunctionAsync("() => !document.getElementById('start').disabled", null, new PageWaitForFunctionOptions { Timeout = 30_000 });
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Start standup" }).ClickAsync();
        await WaitForCloseAsync(page, sessionId, timeout, ct);

        var session = await _http.GetFromJsonAsync<JsonElement>($"/api/sessions/{sessionId}", Json, ct);
        var turns = ReadTurns(session);
        var exchanges = ReadExchanges(session);
        return new RunResult(
            Guid.NewGuid().ToString("N")[..8], label, build, api, startedAt, sessionId, ReadInt(session, "p50LatencyMs"),
            turns.Where(t => t.Reason != "TalkOver").ToList(), team,
            TalkOvers: turns.Count(t => t.Reason == "TalkOver"),
            HumanSegments: exchanges.Count(e => e.MemberId is not null),
            Exchanges: exchanges)
        {
            Retrospective = retrospective ? ReadRetroProof(session) : null,
        };
    }

    // The page shows "Closed" when the standup ends, but in a long multi-team run a page sometimes never repaints it (seen in prompt 4A: the
    // session had ended on the server minutes earlier). The timings come from the server session, so the end is also read from there.
    private async Task WaitForCloseAsync(IPage page, string sessionId, TimeSpan timeout, CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await page.EvaluateAsync<bool>("() => document.getElementById('floor')?.textContent?.startsWith('Closed') === true"))
            {
                return;
            }

            var session = await _http.GetFromJsonAsync<JsonElement>($"/api/sessions/{sessionId}", Json, ct);
            if (session.TryGetProperty("status", out var status) && status.ToString() == "Ended")
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }

        throw new TimeoutException($"The standup {sessionId} did not close within {timeout.TotalMinutes:F0} minutes.");
    }

    /// <inheritdoc />
    public void Dispose() => _http.Dispose();

    private static HttpClient NewClient(string api, string? token)
    {
        var http = new HttpClient { BaseAddress = new Uri(api), Timeout = TimeSpan.FromMinutes(2) };
        if (!string.IsNullOrEmpty(token))
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return http;
    }

    private async Task<string> PostAsync(string path, object body, CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync(path, body, Json, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        return response.IsSuccessStatusCode ? text : throw new InvalidOperationException($"{path} -> {(int)response.StatusCode} {text}");
    }

    private static int ReadInt(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.TryGetInt32(out var i) ? i : 0;

    private static RetroProof ReadRetroProof(JsonElement session)
    {
        var events = session.GetProperty("events").EnumerateArray().ToArray();
        var themes = Array.FindIndex(events, e => e.GetProperty("type").GetString() == "Phase" && e.GetProperty("text").GetString() == "Themes");
        var circles = events.Select((ev, index) => (Event: ev, Index: index))
            .Where(e => e.Event.GetProperty("type").GetString() == "CircledBack").ToArray();
        var members = session.GetProperty("preBrief").GetProperty("members").EnumerateArray().ToArray();
        var spoken = 0;
        var answered = 0;
        foreach (var circle in circles)
        {
            var memberId = circle.Event.GetProperty("memberId").GetString();
            var member = members.First(m => m.GetProperty("memberId").GetString() == memberId);
            var firstName = member.GetProperty("displayName").GetString()!.Split(' ')[0];
            var following = events.Skip(circle.Index + 1).Take(Math.Max(0, themes - circle.Index - 1)).ToArray();
            if (following.Any(e => e.GetProperty("type").GetString() == "Transcript"
                && e.GetProperty("memberId").ValueKind == JsonValueKind.Null
                && e.GetProperty("text").GetString() is { } text
                && text.Contains("circling back", StringComparison.OrdinalIgnoreCase)
                && text.Contains(firstName, StringComparison.OrdinalIgnoreCase))) { spoken++; }
            if (following.Any(e => e.GetProperty("type").GetString() == "RetroItem" && e.GetProperty("memberId").GetString() == memberId)) { answered++; }
        }
        return new RetroProof(session.GetProperty("status").GetString() == "Ended",
            events.Count(e => e.GetProperty("type").GetString() == "Deferred"), circles.Length, spoken, answered,
            events.Count(e => e.GetProperty("type").GetString() == "ActionItem" && !e.GetProperty("retracted").GetBoolean()));
    }

    private static List<TimingTurn> ReadTurns(JsonElement session)
    {
        var turns = new List<TimingTurn>();
        if (!session.TryGetProperty("events", out var events))
        {
            return turns;
        }

        foreach (var e in events.EnumerateArray().Where(e => e.TryGetProperty("type", out var t) && t.ToString() == "Timing"))
        {
            var d = JsonDocument.Parse(e.GetProperty("text").GetString() ?? "{}").RootElement;
            turns.Add(new TimingTurn(
                d.TryGetProperty("reason", out var r) ? r.GetString() ?? "unknown" : "unknown",
                ReadInt(d, "totalMs"),
                ReadInt(d, "speechEndToTranscriptMs"),
                ReadInt(d, "analyserMs"),
                ReadInt(d, "decisionModelMs"),
                ReadInt(d, "orchestrationMs"),
                ReadInt(d, "voiceFirstAudioMs"),
                d.TryGetProperty("source", out var s) ? s.GetString() ?? "none" : "none",
                d.TryGetProperty("traceId", out var trace) ? trace.GetString() : null,
                d.TryGetProperty("spanId", out var span) ? span.GetString() : null));
        }

        return turns;
    }

    // The spoken words in order: a member's id with their transcript, or no member for the agent. Used to score quality and to count how
    // many pieces a person's update arrived in (a pause longer than the silence setting splits it).
    private static List<Exchange> ReadExchanges(JsonElement session)
    {
        var list = new List<Exchange>();
        if (!session.TryGetProperty("events", out var events))
        {
            return list;
        }

        foreach (var e in events.EnumerateArray().Where(e => e.TryGetProperty("type", out var t) && t.ToString() == "Transcript"))
        {
            var member = e.TryGetProperty("memberId", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
            list.Add(new Exchange(member, e.TryGetProperty("text", out var x) ? x.GetString() ?? string.Empty : string.Empty));
        }

        return list;
    }

    private sealed record StartReply(string RoomUrl);
}
