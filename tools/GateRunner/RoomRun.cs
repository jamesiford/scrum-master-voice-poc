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
    /// <returns>The run result.</returns>
    public async Task<RunResult> RunAsync(string label, string build, TimeSpan timeout, CancellationToken ct)
    {
        var startedAt = DateTimeOffset.UtcNow;
        await PostAsync($"/api/teams/{team}/members/sync", new { }, ct);
        var sessionId = (await PostAsync("/api/sessions/prepare", new { teamId = team }, ct)).Trim('"');
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
        await page.WaitForFunctionAsync("() => document.getElementById('floor')?.textContent?.startsWith('Closed')", null, new PageWaitForFunctionOptions { Timeout = (float)timeout.TotalMilliseconds });

        var session = await _http.GetFromJsonAsync<JsonElement>($"/api/sessions/{sessionId}", Json, ct);
        return new RunResult(Guid.NewGuid().ToString("N")[..8], label, build, api, startedAt, sessionId, ReadInt(session, "p50LatencyMs"), ReadTurns(session), team);
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
                d.TryGetProperty("source", out var s) ? s.GetString() ?? "none" : "none"));
        }

        return turns;
    }

    private sealed record StartReply(string RoomUrl);
}
