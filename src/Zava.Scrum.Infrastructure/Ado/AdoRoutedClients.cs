using System.Net.Http.Json;
using System.Text.Json;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;
using Zava.Scrum.Infrastructure.Demo;
using Zava.Scrum.Infrastructure.Persistence;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Infrastructure.Ado;

/// <summary>
/// Reads the work tracker for whichever organisation the team belongs to. It holds no organisation of its own: each call takes the
/// team and goes to <c>team.Ado.Org</c> through the factory (ADR 0018).
/// </summary>
internal sealed class RoutedWorkItemReader(IAdoClientFactory factory) : IWorkItemReader, IIterationReader, IBacklogReader
{
    /// <inheritdoc />
    public Task<Result<SprintSnapshot>> GetCurrentSprintAsync(Team team, CancellationToken ct) => Reader(team).GetCurrentSprintAsync(team, ct);

    /// <inheritdoc />
    public Task<string?> GetNextIterationPathAsync(Team team, CancellationToken ct) => Reader(team).GetNextIterationPathAsync(team, ct);

    /// <inheritdoc />
    public Task<IterationInfo?> FindIterationAsync(Team team, string? name, CancellationToken ct) => Reader(team).FindIterationAsync(team, name, ct);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<WorkItem>>> GetBacklogAsync(Team team, CancellationToken ct) => Reader(team).GetBacklogAsync(team, ct);

    private AdoRestClient Reader(Team team)
    {
        ArgumentNullException.ThrowIfNull(team);
        return factory.ReaderFor(team.Ado.Org);
    }
}

/// <summary>Writes an approved change to the organisation of the team it belongs to, as the approver (ADR 0020).</summary>
internal sealed class RoutedWorkItemWriter(IAdoClientFactory factory, IDemoStateStore publication, DemoAdoResetGate gate) : IWorkItemWriter
{
    /// <inheritdoc />
    public Task<Result<WorkItemRevision>> ApplyAsync(ApprovedChange change, AdoSettings target, DelegatedWriteContext context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(context);
        return gate.RunAsync(() => ApplyAsDelegateAsync(change, target, context, ct), ct);
    }

    private async Task<Result<WorkItemRevision>> ApplyAsDelegateAsync(ApprovedChange change, AdoSettings target, DelegatedWriteContext context, CancellationToken ct)
    {
        var state = await publication.ReadAsync(ct);
        var reset = change.Op == ChangeOp.ResetDemoItem
            ? state.Operations.GetValueOrDefault(change.DemoReset?.OperationId ?? Guid.Empty)
            : state.Operations.Values.FirstOrDefault(operation => !operation.Completed && operation.Scope.Any(team => team.Ado == target));
        if (reset is not null && !reset.Completed)
        {
            var registry = state.Collections.TryGetValue("teams.json", out var json)
                ? json.Deserialize<Dictionary<string, Team>>(JsonFile.SerializerOptions) ?? [] : [];
            if (change.Op == ChangeOp.ResetDemoItem && target != reset.Target.Ado
                || reset.Scope.Any(original => !registry.TryGetValue(original.Id, out var current)
                    || current.Ado != original.Ado || !current.IsDemo || current.DemoTenantId != reset.TenantId))
            {
                return Result<WorkItemRevision>.Failure(Error.Conflict("demo.reset_changed", "The verified demo connection or scope changed after staging. No delegation or Azure DevOps write was attempted."));
            }

            if (state.Sessions.Values.SelectMany(s => s).Any(s => reset.Scope.Any(t => t.Id == s.TeamId)
                && (s.Status == StandupStatus.Live || s.StartedAt is not null && s.EndedAt is null)))
            {
                return Result<WorkItemRevision>.Failure(Error.Conflict("demo.session_running", "A selected demo team has a running take; no Azure DevOps baseline was written."));
            }

            var baseline = reset.Approval.Changes.SingleOrDefault(proposal => proposal.WorkItemId == change.WorkItemId);
            if (change.Op != ChangeOp.ResetDemoItem || change.DemoReset?.OperationId != reset.Id || baseline?.DemoReset is null
                || JsonSerializer.Serialize(change.DemoReset) != JsonSerializer.Serialize(baseline.DemoReset))
            {
                return Result<WorkItemRevision>.Failure(Error.Conflict("demo.reset_write_guard", "Only the exact staged reset follow-up may write this demo area until apply/commit finishes."));
            }
        }
        else if (change.Op == ChangeOp.ResetDemoItem)
        {
            return Result<WorkItemRevision>.Failure(Error.Forbidden("demo.reset_write_guard", "A demo baseline write requires its active staged operation."));
        }

        var access = await context.GetAccessAsync(ct);
        return access.IsSuccess
            ? await factory.DelegatedWriterFor(target.Org, access.Value.AccessToken).ApplyAsync(change, ct)
            : Result<WorkItemRevision>.Failure(access.Error!);
    }
}

/// <summary>Removes the agent's comments in the team's project, in the team's organisation.</summary>
internal sealed class RoutedDemoReset(IAdoClientFactory factory) : IDemoAdoReset
{
    /// <inheritdoc />
    public Task<int> RemoveAgentCommentsAsync(AdoSettings ado, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(ado);
        return new AdoDemoReset(factory.For(ado.Org)).RemoveAgentCommentsAsync(ado.Project, ct);
    }
}

/// <summary>Reads where a work item lives, so a write aimed at another team's item can be refused (<see cref="WorkItemScope"/>).</summary>
internal sealed class AdoWorkItemScopeReader(IAdoClientFactory factory) : IWorkItemScopeReader
{
    /// <inheritdoc />
    public async Task<Result<WorkItemLocation>> GetLocationAsync(AdoSettings ado, int workItemId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(ado);
        try
        {
            using var response = await factory.For(ado.Org).GetAsync($"_apis/wit/workitems/{workItemId}?fields=System.TeamProject,System.AreaPath&api-version=7.1", ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Result<WorkItemLocation>.Failure(Error.NotFound("ado.work_item_not_found", $"Work item {workItemId} was not found in {ado.Org}."));
            }

            if (!response.IsSuccessStatusCode)
            {
                return Result<WorkItemLocation>.Failure(Error.ExternalRejected("ado.scope_unreadable", $"Azure DevOps would not say where work item {workItemId} lives ({(int)response.StatusCode}); nothing was written."));
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var fields = doc.RootElement.GetProperty("fields");
            return Result<WorkItemLocation>.Success(new WorkItemLocation(
                fields.TryGetProperty("System.TeamProject", out var p) ? p.GetString() ?? string.Empty : string.Empty,
                fields.TryGetProperty("System.AreaPath", out var a) ? a.GetString() ?? string.Empty : string.Empty));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or Azure.Identity.CredentialUnavailableException or Azure.Identity.AuthenticationFailedException)
        {
            return Result<WorkItemLocation>.Failure(Error.ExternalRejected("ado.unreachable", "Azure DevOps could not be reached or the credential was refused; nothing was written."));
        }
    }
}
