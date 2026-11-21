using System.Text.Json;
using Mcm.Scrum.Tools.SeedAdo;

// Usage: dotnet run --project tools/SeedAdo -- [--org https://dev.azure.com/<org>] [--file demo/seed-ado.json]
// Auth: DefaultAzureCredential (az login). Never pass or store a PAT here.
var org = Arg("--org") ?? Environment.GetEnvironmentVariable("Ado__Org") ?? "https://dev.azure.com/fordjames-demos";
var file = Arg("--file") ?? FindSeedFile();
var tenant = Arg("--tenant") ?? Environment.GetEnvironmentVariable("Azure__TenantId");

var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var seed = JsonSerializer.Deserialize<SeedDefinition>(await File.ReadAllTextAsync(file), options)
    ?? throw new InvalidOperationException($"{file} is empty.");

Console.WriteLine($"Seeding {org} / {seed.Project} from {file}");
using var ado = new AdoClient(org, tenant);
try
{
    await new Seeder(ado, seed, Console.WriteLine).RunAsync(CancellationToken.None);
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

static string FindSeedFile()
{
    for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
    {
        var candidate = Path.Combine(dir.FullName, "demo", "seed-ado.json");
        if (File.Exists(candidate))
        {
            return candidate;
        }
    }

    throw new FileNotFoundException("demo/seed-ado.json not found. Pass --file.");
}
