using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Standups;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure.Ado;

/// <summary>
/// Writes approved changes to Azure DevOps (SPEC 9.4). It accepts only <see cref="ApprovedChange"/>, which only an approved
/// follow-up can produce, and stamps every change with who approved it.
/// </summary>
/// <remarks>VERIFY: https://learn.microsoft.com/rest/api/azure/devops/wit/work-items/update and /create (json-patch, api-version 7.1).</remarks>
internal sealed class AdoWorkItemWriter(HttpClient http, IOptions<DemoOptions> demo, ILogger<AdoWorkItemWriter> logger)
{
    private const string ApiVersion = "api-version=7.1";
    private const string OwnerTagPrefix = "demo-owner-";

    /// <summary>Applies one approved change to this writer's organisation and records who approved it.</summary>
    /// <param name="change">The change.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The revision the write produced, or an external failure.</returns>
    public async Task<Result<WorkItemRevision>> ApplyAsync(ApprovedChange change, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(change);
        try
        {
            var request = await BuildRequestAsync(change, ct);
            if (request is null)
            {
                return Result<WorkItemRevision>.Success(new WorkItemRevision(change.WorkItemId, 0));
            }

            using (request)
            {
                using var response = await http.SendAsync(request, ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                if (response.IsSuccessStatusCode)
                {
                    return Result<WorkItemRevision>.Success(ReadRevision(body, change.WorkItemId));
                }


                logger.LogWarning("Azure DevOps rejected the {Op} change of {WorkItemId}: {Status} {Body}", change.Op, change.WorkItemId, (int)response.StatusCode, body);
                if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.NonAuthoritativeInformation)
                {
                    // The write is made as the approver, so this means the approver cannot change this item (ADR 0020). Nothing was written.
                    return Result<WorkItemRevision>.Failure(Error.Forbidden("ado.approver_cannot_write", $"Azure DevOps will not let you change work item {change.WorkItemId} ({(int)response.StatusCode}). You need Contributor access to its project. Nothing was written."));
                }

                return Result<WorkItemRevision>.Failure(Error.ExternalRejected("ado.write_rejected", $"Azure DevOps rejected the {change.Op} change ({(int)response.StatusCode})."));
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or Azure.Identity.CredentialUnavailableException or Azure.Identity.AuthenticationFailedException)
        {
            logger.LogWarning(ex, "Azure DevOps write failed");
            return Result<WorkItemRevision>.Failure(Error.ExternalRejected("ado.unreachable", "Azure DevOps could not be reached or the credential was refused."));
        }
    }

    // Azure DevOps answers a work item write with the item: "id" and the new "rev". The revision goes into the approval trail.
    private static WorkItemRevision ReadRevision(string body, int requestedId)
    {
        // The write already happened, so an unreadable body must not turn into a failure (a retry would write twice); it yields revision 0, which the audit chain reports as a missing revision.
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return new WorkItemRevision(requestedId, 0);
        }

        using var parsed = doc;
        var id = parsed.RootElement.TryGetProperty("id", out var i) && i.TryGetInt32(out var parsedId) ? parsedId : requestedId;
        var rev = parsed.RootElement.TryGetProperty("rev", out var r) && r.TryGetInt32(out var parsedRev) ? parsedRev : 0;
        return new WorkItemRevision(id, rev);
    }

    private async Task<HttpRequestMessage?> BuildRequestAsync(ApprovedChange change, CancellationToken ct)
    {
        if (change.Op == ChangeOp.CreateWorkItem)
        {
            var item = change.New ?? throw new InvalidOperationException("A create change needs a work item.");
            return Patch(HttpMethod.Post, CreateUrl(item), CreateOperations(change, item));
        }

        var ops = await BuildOperationsAsync(change, ct);
        return ops is null ? null : Patch(HttpMethod.Patch, $"_apis/wit/workitems/{change.WorkItemId}?{ApiVersion}", ops);
    }

    // The first segment of an iteration path is the project, so the new item lands in the project its iteration belongs to.
    private static string CreateUrl(NewWorkItem item) =>
        $"{Uri.EscapeDataString(item.IterationPath.Split('\\')[0])}/_apis/wit/workitems/${Uri.EscapeDataString(item.Type)}?{ApiVersion}";

    // The attribution goes into System.History, which becomes the first comment on the new item.
    private List<object> CreateOperations(ApprovedChange change, NewWorkItem item)
    {
        var ops = new List<object>
        {
            Add(AdoFields.Title, item.Title),
            Add(AdoFields.Description, Encode(item.Description)),
            Add(AdoFields.IterationPath, item.IterationPath),
            Add(AdoFields.History, $"Created from the {change.Ceremony.Phrase()}.<br/><i>{Encode(change.Attribution)}</i>"),
        };
        // DEMO: as for Assign, the sandbox owner is a tag. PROD: AssignedTo only.
        IReadOnlyList<string> tags = demo.Value.HonorOwnerTags && change.OwnerKey is not null ? [.. item.Tags, OwnerTagPrefix + change.OwnerKey] : item.Tags;
        if (tags.Count > 0)
        {
            ops.Add(Add(AdoFields.Tags, string.Join("; ", tags)));
        }

        if (!string.IsNullOrWhiteSpace(item.AssignedTo) && !(demo.Value.HonorOwnerTags && change.OwnerKey is not null))
        {
            ops.Add(Add(AdoFields.AssignedTo, item.AssignedTo));
        }

        return ops;
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
            case ChangeOp.SetIteration:
                ops.Add(Add(AdoFields.IterationPath, change.Value));
                ops.Add(Add(AdoFields.History, $"Moved to {Encode(change.Value)}.<br/><i>{Encode(change.Attribution)}</i>"));
                break;
            case ChangeOp.Assign:
                // DEMO: the sandbox has one real user, so ownership is the demo-owner-{key} tag the reader maps to the cast. PROD: AssignedTo only.
                if (demo.Value.HonorOwnerTags && change.OwnerKey is not null)
                {
                    ops.Add(Add(AdoFields.Tags, await OwnerTagsAsync(change, ct)));
                }
                else
                {
                    ops.Add(Add(AdoFields.AssignedTo, change.Value));
                }

                ops.Add(Add(AdoFields.History, $"Assigned to {Encode(change.Value)}.<br/><i>{Encode(change.Attribution)}</i>"));
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

    private static HttpRequestMessage Patch(HttpMethod method, string url, List<object> ops) =>
        new(method, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(ops), Encoding.UTF8, new MediaTypeHeaderValue("application/json-patch+json")),
        };

    private async Task<string> OwnerTagsAsync(ApprovedChange change, CancellationToken ct)
    {
        var set = await ReadTagsAsync(change.WorkItemId, ct);
        set.RemoveAll(s => s.StartsWith(OwnerTagPrefix, StringComparison.OrdinalIgnoreCase));
        set.Add(OwnerTagPrefix + change.OwnerKey);
        return string.Join("; ", set);
    }

    private async Task<List<string>> ReadTagsAsync(int workItemId, CancellationToken ct)
    {
        using var response = await http.GetAsync($"_apis/wit/workitems/{workItemId}?fields={AdoFields.Tags}&{ApiVersion}", ct);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var current = doc.RootElement.GetProperty("fields").TryGetProperty(AdoFields.Tags, out var t) ? t.GetString() ?? string.Empty : string.Empty;
        return current.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private async Task<string?> MergeTagsAsync(ApprovedChange change, CancellationToken ct)
    {
        var set = await ReadTagsAsync(change.WorkItemId, ct);
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
