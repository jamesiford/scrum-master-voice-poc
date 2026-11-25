using System.Text.Json;
using Zava.Scrum.Tools.SeedAdo;

// Usage: dotnet run --project tools/SeedAdo -- [--org https://dev.azure.com/<org>] [--file demo/seed-ado.json] [--payments-only]
//        dotnet run --project tools/SeedAdo -- --rebrand [--old-project <name>] [--dry-run | --apply]
// --rebrand renames an existing project and its content from the old customer brand to the generic one (docs/rebrand/AZURE-DEVOPS.md).
// It is a dry run unless --apply is given. Run it once, before the first normal seed under the new project name.
// Auth: DefaultAzureCredential (az login). Never pass or store a PAT here.
var org = Arg("--org") ?? Environment.GetEnvironmentVariable("Ado__Org") ?? "https://dev.azure.com/fordjames-demos";
var tenant = Arg("--tenant") ?? Environment.GetEnvironmentVariable("Azure__TenantId");
var rebrand = HasFlag("--rebrand");
// Three demo teams by default (Payments, Identity, Catalogue: one project each in the same organisation). --payments-only seeds Payments alone,
// for the film. --file seeds exactly one definition.
var files = Arg("--file") is { } one ? [one] : SeedFiles(HasFlag("--payments-only") || rebrand);

var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
using var ado = new AdoClient(org, tenant);
try
{
    foreach (var file in files)
    {
        var seed = JsonSerializer.Deserialize<SeedDefinition>(await File.ReadAllTextAsync(file), options)
            ?? throw new InvalidOperationException($"{file} is empty.");
        Console.WriteLine($"{(rebrand ? "Rebranding" : "Seeding")} {org} / {seed.Project} from {file}");
        if (rebrand)
        {
            await new Rebrander(ado, Arg("--old-project") ?? "MCM-Scrum-Demo", seed.Project, HasFlag("--apply") && !HasFlag("--dry-run"), Console.WriteLine).RunAsync(CancellationToken.None);
        }
        else
        {
            await new Seeder(ado, seed, Console.WriteLine).RunAsync(CancellationToken.None);
        }
    }
}
catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or Azure.Identity.AuthenticationFailedException)
{
    await Console.Error.WriteLineAsync($"Seeding failed: {ex.Message}");
    return 1;
}

return 0;
static string? Arg(string name)
{
    var args = Environment.GetCommandLineArgs();
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static bool HasFlag(string name) => Environment.GetCommandLineArgs().Contains(name);

static List<string> SeedFiles(bool paymentsOnly)
{
    for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
    {
        var main = Path.Combine(dir.FullName, "demo", "seed-ado.json");
        if (File.Exists(main))
        {
            var files = new List<string> { main };
            if (!paymentsOnly)
            {
                files.AddRange(Directory.GetFiles(Path.Combine(dir.FullName, "demo"), "seed-ado-*.json").Order(StringComparer.Ordinal));
            }

            return files;
        }
    }

    throw new FileNotFoundException("demo/seed-ado.json not found. Pass --file.");
}
