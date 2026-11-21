using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.FollowUps;
using Microsoft.Extensions.Logging;

namespace Mcm.Scrum.Infrastructure.Ado;

/// <summary>
/// Writes approved changes to Azure DevOps (SPEC 9.4). It accepts only <see cref="ApprovedChange"/>, which only an approved
/// follow-up can produce, and stamps every change with who approved it.
/// </summary>
/// <remarks>VERIFY: https://learn.microsoft.com/rest/api/azure/devops/wit/work-items/update (json-patch, api-version 7.1).</remarks>
public sealed class AdoWorkItemWriter(HttpClient http, ILogger<AdoWorkItemWriter> logger) : IWorkItemWriter
{
    private const string ApiVersion = "api-version=7.1";

    /// <inheritdoc />
    public async Task<Result> ApplyAsync(ApprovedChange change, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(change);
        try
        {
            var ops = await BuildOperationsAsync(change, ct);
            if (ops is null)
            {
                return Result.Success();
            }

            using var request = new HttpRequestMessage(HttpMethod.Patch, $"_apis/wit/workitems/{change.WorkItemId}?{ApiVersion}")
            {
                Content = new StringContent(JsonSerializer.Serialize(ops), Encoding.UTF8, new MediaTypeHeaderValue("application/json-patch+json")),
            };
            using var response = await http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Azure DevOps rejected the update of {WorkItemId}: {Status} {Body}", change.WorkItemId, (int)response.StatusCode, body);
            return Result.Failure(Error.ExternalRejected("ado.write_rejected", $"Azure DevOps rejected the update of work item {change.WorkItemId} ({(int)response.StatusCode})."));
        }
        catch (Exception ex) when (ex is HttpRequestException or Azure.Identity.CredentialUnavailableException or Azure.Identity.AuthenticationFailedException)
        {
            logger.LogWarning(ex, "Azure DevOps write failed");
            return Result.Failure(Error.ExternalRejected("ado.unreachable", "Azure DevOps could not be reached or the credential was refused."));
        }
    }

    private async Task<List<object>?> BuildOperationsAsync(ApprovedChange change, CancellationToken ct)
    {
        var ops = new List<object>();
        switch (change.Op)
        {
            case ChangeOp.Comment:
                ops.Add(Add(AdoFields.History, $"{Encode(change.Value)}<br/><i>{Encode(change.Attribution)}</i>"));
                break;
            case ChangeOp.State:
                ops.Add(Add(AdoFields.State, change.Value));
                ops.Add(Add(AdoFields.History, $"State set to {Encode(change.Value)}.<br/><i>{Encode(change.Attribution)}</i>"));
                break;
            case ChangeOp.Tag:
                var tags = await MergeTagsAsync(change, ct);
                if (tags is null)
                {
                    return null;
                }

                ops.Add(Add(AdoFields.Tags, tags));
                ops.Add(Add(AdoFields.History, $"Tag change {Encode(change.Value)}.<br/><i>{Encode(change.Attribution)}</i>"));
                break;
        }

        return ops;
    }

    private async Task<string?> MergeTagsAsync(ApprovedChange change, CancellationToken ct)
    {
        using var response = await http.GetAsync($"_apis/wit/workitems/{change.WorkItemId}?fields={AdoFields.Tags}&{ApiVersion}", ct);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var current = doc.RootElement.GetProperty("fields").TryGetProperty(AdoFields.Tags, out var t) ? t.GetString() ?? string.Empty : string.Empty;
        var set = current.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        var name = change.Value[1..].Trim();
        var present = set.Contains(name, StringComparer.OrdinalIgnoreCase);
        if (change.Value[0] == '+' && !present)
        {
            set.Add(name);
        }
        else if (change.Value[0] == '-' && present)
        {
            set.RemoveAll(s => s.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            return null;
        }

        return string.Join("; ", set);
    }

    private static object Add(string field, string value) => new { op = "add", path = $"/fields/{field}", value };

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
