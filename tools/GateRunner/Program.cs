using System.Diagnostics;
using System.Text.Json;
using Zava.Scrum.Tools.GateRunner;

// Usage:
//   dotnet run --project tools/GateRunner -- --label decision [--runs 3] [--api http://localhost:5080] [--team team-payments]
//        [--teams a,b,c] [--out docs/gates/baseline-M10] [--build <id>] [--token <bearer>] [--timeout-minutes 12] [--check-audit]
//   dotnet run --project tools/GateRunner -- --summarise docs/gates/baseline-M10 [--only-team team-payments]
// Drives the Simulated Room cast (demo/cast/payments-standup.yaml) through the real API and records per-turn timing by hop.
// The configuration under test (Engine=Decision or Mode=Rules) is set on the API process, not here; --label only names it.
var label = Arg("--label") ?? "run";
var api = (Arg("--api") ?? "http://localhost:5080").TrimEnd('/');
var team = Arg("--team") ?? "team-payments";
var outDir = Arg("--out") ?? Path.Combine("docs", "gates", "baseline-M10");
var runs = int.TryParse(Arg("--runs"), out var n) ? n : 1;
var timeout = TimeSpan.FromMinutes(int.TryParse(Arg("--timeout-minutes"), out var m) ? m : 12);

if (Arg("--evaluate") is { } goldens)
{
    return await DecisionEvaluation.RunAsync(goldens, outDir, Has("--model"), Has("--release-check"), CancellationToken.None);
}

if (Arg("--summarise") is { } dir)
{
    Console.WriteLine(Summarise(dir, Arg("--only-team")));
    return 0;
}

var build = Arg("--build") ?? Environment.GetEnvironmentVariable("GATE_BUILD") ?? GitHead();
Directory.CreateDirectory(outDir);
var failed = false;
var token = Arg("--token") ?? Environment.GetEnvironmentVariable("GATE_TOKEN");
// --teams a,b,c runs one standup per team at the same time (G9); without it, one standup on --team.
var teamList = (Arg("--teams") ?? team).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
if (teamList.Length > 1)
{
    await TeamSetup.EnsureAsync(api, token, team, teamList, CancellationToken.None);
}

for (var i = 1; i <= runs; i++)
{
    Console.WriteLine($"Run {i} of {runs}: label {label}, build {build}, api {api}, teams {string.Join(',', teamList)}");
    var wall = Stopwatch.StartNew();
    var results = await Task.WhenAll(teamList.Select(async t =>
    {
        using var room = new RoomRun(api, t, token);
        return (Team: t, Result: await room.RunAsync(label, build, timeout, CancellationToken.None));
    }));
    foreach (var (t, result) in results)
    {
        var file = Path.Combine(outDir, $"{label}-{t}-{result.StartedAt:yyyyMMdd-HHmmss}-{result.RunId}.json");
        await File.WriteAllTextAsync(file, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"  {t}: {result.Turns.Count} timed turns, API p50 {result.ReportedP50Ms} ms, saved {file}");
        if (Has("--check-audit") && !await AuditCheck.RunAsync(api, token, t, result.SessionId))
        {
            failed = true;
        }
    }

    Console.WriteLine($"  {teamList.Length} concurrent standups finished in {wall.Elapsed.TotalSeconds:F0} s");
}
var summary = Summarise(outDir);
await File.WriteAllTextAsync(Path.Combine(outDir, "summary.md"), summary);
Console.WriteLine();
Console.WriteLine(summary);
Checklist.Print();
return failed ? 1 : 0;

static string GitHead()
{
    try
    {
        using var p = Process.Start(new ProcessStartInfo("git", "rev-parse --short HEAD") { RedirectStandardOutput = true, UseShellExecute = false })!;
        var id = p.StandardOutput.ReadToEnd().Trim();
        p.WaitForExit();
        return id.Length > 0 ? id : "unknown";
    }
    catch (System.ComponentModel.Win32Exception)
    {
        return "unknown";
    }
}

static string Summarise(string directory, string? onlyTeam = null)
{
    var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    var runs = Directory.GetFiles(directory, "*.json")
        .Select(f => JsonSerializer.Deserialize<RunResult>(File.ReadAllText(f), options))
        .OfType<RunResult>()
        .Where(r => onlyTeam is null || r.Team == onlyTeam)
        .ToList();
    return SummaryWriter.Write(runs);
}

string? Arg(string name)
{
    var args = Environment.GetCommandLineArgs();
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

bool Has(string name) => Environment.GetCommandLineArgs().Contains(name);
