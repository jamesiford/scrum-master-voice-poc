using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Zava.Scrum.Tools.GateRunner;

/// <summary>Makes sure the teams a concurrency run needs exist and are active, registering any that are missing through the team registry API.</summary>
internal static class TeamSetup
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Registers each missing team as a copy of <paramref name="template"/> (same Azure DevOps connection) and sets it Active.</summary>
    /// <param name="api">The API base URL.</param>
    /// <param name="token">An Admin bearer token, or <see langword="null"/>.</param>
    /// <param name="template">The existing team whose connection and settings are copied.</param>
    /// <param name="teams">The team ids the run needs.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    public static async Task EnsureAsync(string api, string? token, string template, IEnumerable<string> teams, CancellationToken ct)
    {
        using var http = new HttpClient { BaseAddress = new Uri(api), Timeout = TimeSpan.FromMinutes(2) };
        if (!string.IsNullOrEmpty(token))
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var source = JsonNode.Parse(await http.GetStringAsync($"/api/teams/{template}", ct))!;
        foreach (var id in teams.Where(t => t != template))
        {
            var input = new
            {
                name = $"Gate {id}",
                timeZone = source["timeZone"]!.GetValue<string>(),
                standupMinutes = source["standupMinutes"]!.GetValue<int>(),
                ado = source["ado"],
                sprintGoal = source["sprintGoal"]!.GetValue<string>(),
            };
            using var exists = await http.GetAsync($"/api/teams/{id}", ct);
            if (!exists.IsSuccessStatusCode)
            {
                using var created = await http.PostAsJsonAsync("/api/teams", new { teamId = id, input }, Json, ct);
                Check(created, $"create {id}", await created.Content.ReadAsStringAsync(ct));
            }

            using var activated = await http.PutAsJsonAsync($"/api/teams/{id}", new { input.name, input.timeZone, input.standupMinutes, input.ado, input.sprintGoal, status = "Active" }, Json, ct);
            Check(activated, $"activate {id}", await activated.Content.ReadAsStringAsync(ct));
        }
    }

    private static void Check(HttpResponseMessage response, string what, string body)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"{what} -> {(int)response.StatusCode} {body}");
        }
    }
}
