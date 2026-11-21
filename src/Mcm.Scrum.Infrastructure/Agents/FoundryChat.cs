using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mcm.Scrum.Infrastructure.Agents;

/// <summary>
/// Calls the Foundry OpenAI-compatible chat route with an Entra token (no keys) and returns JSON text.
/// Any failure yields <see langword="null"/> so callers fall back to deterministic text.
/// </summary>
/// <remarks>VERIFY: https://learn.microsoft.com/azure/ai-foundry/openai/latest (v1 chat completions on the Foundry resource host).</remarks>
public sealed class FoundryChat(HttpClient http, IOptions<FoundryOptions> options, ILogger<FoundryChat> logger)
{
    /// <summary>Gets a value indicating whether a model deployment is configured.</summary>
    public bool IsConfigured => options.Value.IsConfigured;

    /// <summary>Asks the reasoning model for a JSON object.</summary>
    /// <param name="system">System instructions.</param>
    /// <param name="user">User content, usually JSON facts.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The JSON text, or <see langword="null"/> when unconfigured or the call failed.</returns>
    public async Task<string?> TryCompleteJsonAsync(string system, string user, CancellationToken ct)
    {
        if (!IsConfigured)
        {
            return null;
        }

        var request = new
        {
            model = options.Value.ReasoningDeployment,
            messages = new object[] { new { role = "system", content = system }, new { role = "user", content = user } },
            response_format = new { type = "json_object" },
            max_completion_tokens = 4000,
        };

        try
        {
            using var response = await http.PostAsJsonAsync("openai/v1/chat/completions", request, ct);
            response.EnsureSuccessStatusCode();
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }
        catch (Exception ex) when (!ct.IsCancellationRequested
            && ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException
                or Azure.Identity.AuthenticationFailedException or Azure.Identity.CredentialUnavailableException)
        {
            logger.LogWarning(ex, "Foundry call failed; using deterministic text");
            return null;
        }
    }
}
