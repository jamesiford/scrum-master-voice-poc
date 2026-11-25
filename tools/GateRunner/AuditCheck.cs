using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Zava.Scrum.Tools.GateRunner;

/// <summary>
/// The traceability gate (G3): after a run, one proposal is approved through the API as the signed-in caller and the audit endpoint must
/// show a complete chain from the session to the Azure DevOps write: the session and its correlation id, the proposal and its hash, the
/// approval record with the approver's Entra identity, and the write record with the work item revisions. Any missing link fails the gate.
/// </summary>
internal static class AuditCheck
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Runs the check.</summary>
    /// <param name="api">The API base address.</param>
    /// <param name="token">A bearer token for a caller holding the Approver and Reader roles, or <see langword="null"/> against a local Development API.</param>
    /// <param name="team">The team.</param>
    /// <param name="sessionId">The session the run produced.</param>
    /// <returns><see langword="true"/> when the chain is complete.</returns>
    public static async Task<bool> RunAsync(string api, string? token, string team, string sessionId)
    {
        using var http = new HttpClient { BaseAddress = new Uri(api + "/"), Timeout = TimeSpan.FromMinutes(2) };
        if (!string.IsNullOrEmpty(token))
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        try
        {
            var session = await http.GetFromJsonAsync<JsonElement>($"api/sessions/{sessionId}", Json);
            var correlation = session.TryGetProperty("correlationId", out var c) ? c.GetString() : null;
            if (string.IsNullOrEmpty(correlation))
            {
                return Fail(sessionId, "the session has no correlation id");
            }

            var followUpId = await ProposeAndApproveAsync(http, team, sessionId);
            if (followUpId is null)
            {
                return Fail(sessionId, "could not propose and approve a follow-up through the API");
            }

            var chain = await WaitForChainAsync(http, correlation);
            return Verify(sessionId, correlation, followUpId, chain);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return Fail(sessionId, ex.Message);
        }
    }

    private static async Task<string?> ProposeAndApproveAsync(HttpClient http, string team, string sessionId)
    {
        var members = await http.GetFromJsonAsync<JsonElement>($"api/teams/{team}/members", Json);
        var member = members.EnumerateArray().FirstOrDefault().TryGetProperty("id", out var m) ? m.GetString() : null;
        var marker = $"gate-{Guid.NewGuid():N}"[..13];
        var proposal = await http.PostAsJsonAsync("api/followups/propose", new { sessionId, memberId = member, workItemId = 1, op = "Comment", value = marker, rationale = "traceability gate" }, Json);
        if (!proposal.IsSuccessStatusCode)
        {
            return null;
        }

        var id = (await proposal.Content.ReadAsStringAsync()).Trim('"');
        using var approval = await http.PostAsJsonAsync($"api/followups/{id}/approve", new { }, Json);
        return approval.IsSuccessStatusCode ? id : null;
    }

    private static async Task<JsonElement> WaitForChainAsync(HttpClient http, string correlation)
    {
        JsonElement chain = default;
        for (var i = 0; i < 20; i++)
        {
            chain = await http.GetFromJsonAsync<JsonElement>($"api/audit/{correlation}", Json);
            var records = chain.GetProperty("records").EnumerateArray();
            if (records.Any(r => r.GetProperty("kind").GetString() == "Applied"))
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return chain;
    }

    private static bool Verify(string sessionId, string correlation, string followUpId, JsonElement chain)
    {
        var missing = chain.GetProperty("missing").EnumerateArray().Select(m => m.GetString() ?? string.Empty).ToList();
        var records = chain.GetProperty("records").EnumerateArray().Where(r => r.GetProperty("followUpId").GetString() == followUpId).ToList();
        var approved = records.FirstOrDefault(r => r.GetProperty("kind").GetString() == "Approved");
        var applied = records.FirstOrDefault(r => r.GetProperty("kind").GetString() == "Applied");
        if (approved.ValueKind == JsonValueKind.Undefined)
        {
            missing.Add("approval record for the gate's follow-up");
        }
        else if (!Guid.TryParse(approved.GetProperty("actor").GetProperty("objectId").GetString(), out var oid) || oid == Guid.Empty)
        {
            missing.Add("approver Entra object id");
        }

        if (applied.ValueKind == JsonValueKind.Undefined || applied.GetProperty("adoRevisions").GetArrayLength() == 0)
        {
            missing.Add("Azure DevOps write record with a revision");
        }

        if (chain.GetProperty("session").ValueKind == JsonValueKind.Null)
        {
            missing.Add("session");
        }

        if (missing.Count > 0)
        {
            return Fail(sessionId, "missing: " + string.Join("; ", missing));
        }

        Console.WriteLine($"  audit chain complete for {chain.GetProperty("tag").GetString()} ({correlation}): {records.Count} records, approver {approved.GetProperty("actor").GetProperty("displayName").GetString()}");
        return true;
    }

    private static bool Fail(string sessionId, string reason)
    {
        Console.WriteLine($"AUDIT CHECK FAILED for session {sessionId}: {reason}");
        return false;
    }
}
