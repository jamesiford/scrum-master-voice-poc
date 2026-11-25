using System.Diagnostics;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;

// Creates an immutable version of the hosted agent and waits until it is active (ADR 0021). Used by infra/hooks/agent-host.ps1.
//   dotnet run --project tools/AgentDeploy -- --endpoint <project endpoint> --name <agent> --image <image> --tenant <id> [--env K=V ...] [--cpu 1] [--memory 2Gi]
// Prints one line, "VERSION=<n>", when the version is active.
var arguments = args.ToList();
string? Arg(string name) => arguments.IndexOf(name) is var i and >= 0 && i + 1 < arguments.Count ? arguments[i + 1] : null;
var endpoint = Arg("--endpoint") ?? throw new ArgumentException("--endpoint is required");
var name = Arg("--name") ?? throw new ArgumentException("--name is required");
var image = Arg("--image") ?? throw new ArgumentException("--image is required");
var envs = arguments.Select((a, i) => (a, i)).Where(t => t.a == "--env" && t.i + 1 < arguments.Count).Select(t => arguments[t.i + 1]).ToList();

var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = Arg("--tenant") });
var agents = new AIProjectClient(new Uri(endpoint), credential).AgentAdministrationClient;
HostedAgentDefinition definition = new(cpu: Arg("--cpu") ?? "1", memory: Arg("--memory") ?? "2Gi")
{
    Versions =
    {
        new ProtocolVersionRecord(ProjectsAgentProtocol.Invocations, "2.0.0"),
        new ProtocolVersionRecord(ProjectsAgentProtocol.InvocationsWs, "2.0.0"),
    },
    ContainerConfiguration = new ContainerConfiguration(image),
};
foreach (var pair in envs)
{
    var split = pair.IndexOf('=', StringComparison.Ordinal);
    definition.EnvironmentVariables[pair[..split]] = pair[(split + 1)..];
}

var clock = Stopwatch.StartNew();
var version = agents.CreateAgentVersion(name, new ProjectsAgentVersionCreationOptions(definition)).Value;
Console.Error.WriteLine($"created version {version.Version} in {clock.ElapsedMilliseconds} ms");
while (true)
{
    var current = agents.GetAgentVersion(name, version.Version).Value;
    if (current.Status == AgentVersionStatus.Active)
    {
        Console.Error.WriteLine($"active after {clock.ElapsedMilliseconds} ms");
        Console.WriteLine($"VERSION={version.Version}");
        return 0;
    }

    if (current.Status == AgentVersionStatus.Failed)
    {
        Console.Error.WriteLine("version failed");
        return 1;
    }

    await Task.Delay(3000);
}
