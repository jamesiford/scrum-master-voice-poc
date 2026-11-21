using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Core;
using Azure.Identity;

namespace Mcm.Scrum.Tools.SeedAdo;

/// <summary>Thin Azure DevOps REST client for the seeder: Entra token via DefaultAzureCredential, JSON in and out.</summary>
internal sealed class AdoClient : IDisposable
{
    private const string Scope = "499b84ac-1321-427f-aa17-267ca6975798/.default";
    private const string ApiVersion = "api-version=7.1";
    private readonly HttpClient _http;
    private readonly TokenCredential _credential;

    public AdoClient(string org, string? tenantId)
    {
        _http = new HttpClient { BaseAddress = new Uri(org.TrimEnd('/') + "/") };
        _credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = tenantId });
    }

    public void Dispose() => _http.Dispose();

    public async Task<JsonNode?> GetAsync(string path, CancellationToken ct, bool allowNotFound = false)
    {
        using var response = await SendAsync(HttpMethod.Get, path, null, "application/json", ct);
        if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return await ReadAsync(response, ct);
    }

    public async Task<JsonNode?> SendJsonAsync(HttpMethod method, string path, object body, CancellationToken ct, bool patch = false)
    {
        using var response = await SendAsync(method, path, body, patch ? "application/json-patch+json" : "application/json", ct);
        return await ReadAsync(response, ct);
    }

    /// <summary>Sends a request and returns the body even on failure, so callers can recognise "already exists".</summary>
    public async Task<(HttpStatusCode Status, string Body)> TrySendJsonAsync(HttpMethod method, string path, object body, CancellationToken ct)
    {
        using var response = await SendAsync(method, path, body, "application/json", ct);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    public static string Url(string path) =>
        path.Contains("api-version=", StringComparison.Ordinal) ? path
        : path.Contains('?', StringComparison.Ordinal) ? $"{path}&{ApiVersion}" : $"{path}?{ApiVersion}";

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, string contentType, CancellationToken ct)
    {
        var token = await _credential.GetTokenAsync(new TokenRequestContext([Scope]), ct);
        var request = new HttpRequestMessage(method, Url(path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, contentType);
        }

        return await _http.SendAsync(request, ct);
    }

    private static async Task<JsonNode?> ReadAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var text = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"{(int)response.StatusCode} {response.StatusCode} for {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}: {text}");
        }

        return string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
    }
}
