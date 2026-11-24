using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using Azure.AI.AgentServer.Invocations;

// S3 spike: the smallest hosted agent that serves /invocations (HTTP) and /invocations_ws (WebSocket).
// It reports how long the process took to start and whether files written to $HOME survive an idle period.
var started = DateTimeOffset.UtcNow;
var marker = Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "/tmp", "spike-marker.txt");
var restored = File.Exists(marker) ? File.ReadAllText(marker) : null;
if (restored is null)
{
    File.WriteAllText(marker, started.ToString("O"));
}

InvocationsServer.Run<SpikeHandler>();

public sealed class SpikeHandler : InvocationWebSocketHandler
{
    private static readonly DateTimeOffset Started = DateTimeOffset.UtcNow;
    private static readonly string Marker = Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "/tmp", "spike-marker.txt");

    public override async Task HandleAsync(HttpRequest request, HttpResponse response, InvocationContext context, CancellationToken cancellationToken)
    {
        var probe = request.Query["probe"].ToString();
        if (probe == "env")
        {
            var names = Environment.GetEnvironmentVariables().Keys.Cast<string>().Where(k => !k.Contains("KEY", StringComparison.OrdinalIgnoreCase) && !k.Contains("SECRET", StringComparison.OrdinalIgnoreCase) && !k.Contains("CONNECTION", StringComparison.OrdinalIgnoreCase)).OrderBy(k => k).Select(k => k + "=" + (k.StartsWith("IDENTITY") || k.StartsWith("MSI") || k.StartsWith("FOUNDRY") || k.StartsWith("AZURE") ? Environment.GetEnvironmentVariable(k) : "...")).ToList();
            await response.WriteAsync(string.Join("\n", names), cancellationToken);
            return;
        }

        if (probe == "ado")
        {
            await response.WriteAsync(await AdoProbe(request.Query["org"].ToString(), request.Query["project"].ToString(), cancellationToken), cancellationToken);
            return;
        }

        var marker = File.Exists(Marker) ? File.ReadAllText(Marker) : "none";
        await response.WriteAsync($"{{\"session\":\"{context.SessionId}\",\"processStarted\":\"{Started:O}\",\"homeMarker\":\"{marker}\",\"now\":\"{DateTimeOffset.UtcNow:O}\"}}", cancellationToken);
    }

    public override async Task HandleWebSocketAsync(WebSocket webSocket, InvocationContext context, CancellationToken cancellationToken)
    {
        var buffer = new byte[64 * 1024];
        while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var received = await webSocket.ReceiveAsync(buffer, cancellationToken);
            if (received.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            // Echo binary frames untouched (the audio hop) and stamp text frames with the server clock (the control hop).
            if (received.MessageType == WebSocketMessageType.Text)
            {
                var text = Encoding.UTF8.GetString(buffer, 0, received.Count);
                var reply = Encoding.UTF8.GetBytes($"{{\"echo\":{System.Text.Json.JsonSerializer.Serialize(text)},\"server\":\"{DateTimeOffset.UtcNow:O}\"}}");
                await webSocket.SendAsync(reply, WebSocketMessageType.Text, true, cancellationToken);
            }
            else
            {
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, received.Count), WebSocketMessageType.Binary, received.EndOfMessage, cancellationToken);
            }
        }
    }

    private static async Task<string> AdoProbe(string org, string project, CancellationToken ct)
    {
        var sb = new System.Text.StringBuilder();
        try
        {
            var cred = new Azure.Identity.ManagedIdentityCredential();
            var token = (await cred.GetTokenAsync(new Azure.Core.TokenRequestContext(["499b84ac-1321-427f-aa17-267ca6975798/.default"]), ct)).Token;
            sb.AppendLine("token acquired, length " + token.Length);
            var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
            var claims = System.Text.Json.JsonDocument.Parse(Convert.FromBase64String(payload.PadRight((payload.Length + 3) / 4 * 4, '=')));
            foreach (var c in new[] { "appid", "oid", "idtyp", "tid", "sub", "xms_act_fct", "xms_sub_fct" })
            {
                if (claims.RootElement.TryGetProperty(c, out var v)) { sb.AppendLine($"claim {c}={v}"); }
            }

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new("Bearer", token);
            var r = await http.GetAsync($"https://dev.azure.com/{org}/_apis/projects?api-version=7.1", ct);
            sb.AppendLine($"GET projects -> {(int)r.StatusCode}");
            r = await http.GetAsync($"https://dev.azure.com/{org}/{project}/_apis/wit/workitems?ids=1&api-version=7.1", ct);
            sb.AppendLine($"GET work item 1 -> {(int)r.StatusCode}");
            var patch = new StringContent("[{\"op\":\"add\",\"path\":\"/fields/System.Title\",\"value\":\"S4 spike by agent identity\"}]", System.Text.Encoding.UTF8, "application/json-patch+json");
            r = await http.PostAsync($"https://dev.azure.com/{org}/{project}/_apis/wit/workitems/$Task?api-version=7.1", patch, ct);
            sb.AppendLine($"POST create work item -> {(int)r.StatusCode} {(r.IsSuccessStatusCode ? "" : (await r.Content.ReadAsStringAsync(ct))[..Math.Min(160, (await r.Content.ReadAsStringAsync(ct)).Length)])}");
        }
        catch (Exception ex)
        {
            sb.AppendLine("ERR " + ex.GetType().Name + ": " + ex.Message[..Math.Min(300, ex.Message.Length)]);
        }

        return sb.ToString();
    }
}
