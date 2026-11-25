using System.Diagnostics;
using System.Text.Json;
using Zava.Scrum.Tools.GateRunner;

// Usage:
//   dotnet run --project tools/GateRunner -- --label decision [--runs 3] [--api http://localhost:5080] [--team team-payments]
//        [--out docs/gates/baseline-M10] [--build <id>] [--token <bearer>] [--timeout-minutes 12] [--check-audit]
//   dotnet run --project tools/GateRunner -- --summarise docs/gates/baseline-M10
// Drives the Simulated Room cast (demo/cast/payments-standup.yaml) through the real API and records per-turn timing by hop.
// The configuration under test (Engine=Decision or Mode=Rules) is set on the API process, not here; --label only names it.
var label = Arg("--label") ?? "run";
var api = (Arg("--api") ?? "http://localhost:5080").TrimEnd('/');
var team = Arg("--team") ?? "team-payments";
var outDir = Arg("--out") ?? Path.Combine("docs", "gates", "baseline-M10");
var runs = int.TryParse(Arg("--runs"), out var n) ? n : 1;
var timeout = TimeSpan.FromMinutes(int.TryParse(Arg("--timeout-minutes"), out var m) ? m : 12);

if (Arg("--summarise") is { } dir)
{
    Console.WriteLine(Summarise(dir));
    return 0;
}

var build = Arg("--build") ?? Environment.GetEnvironmentVariable("GATE_BUILD") ?? GitHead();
Directory.CreateDirectory(outDir);
var failed = false;
for (var i = 1; i <= runs; i++)
{
    Console.WriteLine($"Run {i} of {runs}: label {label}, build {build}, api {api}");
    using var room = new RoomRun(api, team, Arg("--token") ?? Environment.GetEnvironmentVariable("GATE_TOKEN"));
    var result = await room.RunAsync(label, build, timeout, CancellationToken.None);
    var file = Path.Combine(outDir, $"{label}-{result.StartedAt:yyyyMMdd-HHmmss}-{result.RunId}.json");
    await File.WriteAllTextAsync(file, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"  {result.Turns.Count} timed turns, API p50 {result.ReportedP50Ms} ms, saved {file}");
    if (Has("--check-audit") && !await AuditCheck.RunAsync(api, Arg("--token") ?? Environment.GetEnvironmentVariable("GATE_TOKEN"), team, result.SessionId))
    {
        failed = true;
    }
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

static string Summarise(string directory)
{
    var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    var runs = Directory.GetFiles(directory, "*.json")
        .Select(f => JsonSerializer.Deserialize<RunResult>(File.ReadAllText(f), options))
        .OfType<RunResult>()
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
