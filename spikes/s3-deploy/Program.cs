using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Core;
using Azure.Identity;

#pragma warning disable AAIP001

// S3 spike: deploy the echo hosted agent, then measure version provisioning, first-request (cold) and next-request (warm) time,
// a WebSocket round trip, and whether $HOME survives an idle period.
var endpoint = "https://fdy-mcm-scrum-demo.services.ai.azure.com/api/projects/proj-mcm-scrum-demo-wus3";
var image = Environment.GetEnvironmentVariable("S3_IMAGE") ?? "acrmcmscrumwiezudy6mumfg.azurecr.io/spikes/s3-hosted:v1";
var name = "s3-spike";
var cred = new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID") });
var client = new AIProjectClient(new Uri(endpoint), cred);
var agents = client.AgentAdministrationClient;

var sw = Stopwatch.StartNew();
HostedAgentDefinition def = new(cpu: "1", memory: "2Gi")
{
    Versions =
    {
        new ProtocolVersionRecord(ProjectsAgentProtocol.Invocations, "2.0.0"),
        new ProtocolVersionRecord(ProjectsAgentProtocol.InvocationsWs, "2.0.0"),
    },
    ContainerConfiguration = new ContainerConfiguration(image),
};
var version = agents.CreateAgentVersion(name, new ProjectsAgentVersionCreationOptions(def)).Value;
Console.WriteLine($"created version {version.Version} in {sw.ElapsedMilliseconds} ms, status {version.Status}");
while (true)
{
    var v = agents.GetAgentVersion(name, version.Version).Value;
    if (v.Status == AgentVersionStatus.Active) { Console.WriteLine($"ACTIVE after {sw.ElapsedMilliseconds} ms"); break; }
    if (v.Status == AgentVersionStatus.Failed) { Console.WriteLine("FAILED " + v.Status); return; }
    await Task.Delay(3000);
}

string Token() => cred.GetToken(new TokenRequestContext(["https://ai.azure.com/.default"])).Token;
using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
var session = Guid.NewGuid().ToString();

async Task<string> Call(string label)
{
    http.DefaultRequestHeaders.Authorization = new("Bearer", Token());
    var t = Stopwatch.StartNew();
    var r = await http.PostAsync($"{endpoint}/agents/{name}/endpoint/protocols/invocations?api-version=v1&agent_session_id={session}", new StringContent("{\"message\":\"hi\"}", Encoding.UTF8, "application/json"));
    var body = await r.Content.ReadAsStringAsync();
    Console.WriteLine($"{label}: {(int)r.StatusCode} in {t.ElapsedMilliseconds} ms  {body}");
    return body;
}

await Call("cold first request");
for (var i = 1; i <= 3; i++) { await Call($"warm request {i}"); }

// WebSocket: connect time and a 640-byte (20 ms PCM16 at 16 kHz) binary frame round trip.
using var ws = new ClientWebSocket();
ws.Options.SetRequestHeader("Authorization", "Bearer " + Token());
var wsUrl = $"wss://fdy-mcm-scrum-demo.services.ai.azure.com/api/projects/proj-mcm-scrum-demo-wus3/agents/{name}/endpoint/protocols/invocations_ws?api-version=v1&agent_session_id={session}";
var c = Stopwatch.StartNew();
await ws.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
Console.WriteLine($"ws connect {c.ElapsedMilliseconds} ms");
var buf = new byte[4096];
var rtts = new List<long>();
for (var i = 0; i < 50; i++)
{
    var frame = new byte[640];
    var t = Stopwatch.StartNew();
    await ws.SendAsync(frame, WebSocketMessageType.Binary, true, CancellationToken.None);
    await ws.ReceiveAsync(buf, CancellationToken.None);
    rtts.Add(t.ElapsedMilliseconds);
}

rtts.Sort();
Console.WriteLine($"ws 640-byte binary round trip over 50: p50 {rtts[25]} ms, p95 {rtts[47]} ms, max {rtts[^1]} ms");
await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);

Console.WriteLine($"session {session}");
File.WriteAllText("last-session.txt", session);
