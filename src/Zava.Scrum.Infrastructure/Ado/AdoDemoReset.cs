using System.Net.Http.Json;
using System.Text.Json;
using Zava.Scrum.Application.Abstractions.Ports;

namespace Zava.Scrum.Infrastructure.Ado;

/// <summary>Deletes the comments this agent wrote. Only the writing identity may delete a comment, which is why this lives in the API.</summary>
/// <remarks>VERIFY: https://learn.microsoft.com/rest/api/azure/devops/wit/comments (list, delete; api-version 7.1-preview.4).</remarks>
public sealed class AdoDemoReset(HttpClient http)
{
    private const string ListVersion = "api-version=7.1-preview.4";
    private const string StampStart = "Approved by";
    private const string StampAgent = "Scrum Agent";

    /// <inheritdoc />
    public async Task<int> RemoveAgentCommentsAsync(string project, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(project);
        var p = Uri.EscapeDataString(project);
        var wiql = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '{project.Replace("'", "''", StringComparison.Ordinal)}'";
        using var query = await http.PostAsJsonAsync($"{p}/_apis/wit/wiql?api-version=7.1", new { query = wiql }, ct);
        query.EnsureSuccessStatusCode();
        using var ids = await JsonDocument.ParseAsync(await query.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var deleted = 0;
        foreach (var id in ids.RootElement.GetProperty("workItems").EnumerateArray().Select(w => w.GetProperty("id").GetInt32()))
        {
            using var list = await http.GetAsync($"{p}/_apis/wit/workItems/{id}/comments?{ListVersion}", ct);
            list.EnsureSuccessStatusCode();
            using var doc = await JsonDocument.ParseAsync(await list.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            foreach (var c in doc.RootElement.GetProperty("comments").EnumerateArray())
            {
                var text = c.TryGetProperty("text", out var t) ? t.GetString() ?? string.Empty : string.Empty;
                if (!text.Contains(StampStart, StringComparison.Ordinal) || !text.Contains(StampAgent, StringComparison.Ordinal))
                {
                    continue;
                }

                using var del = await http.DeleteAsync($"{p}/_apis/wit/workItems/{id}/comments/{c.GetProperty("id").GetInt32()}?{ListVersion}", ct);
                if (del.IsSuccessStatusCode)
                {
                    deleted++;
                }
            }
        }

        return deleted;
    }
}
