using System.Net.Http.Json;
using System.Text.Json;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Infrastructure.Ado;

/// <summary>
/// Reads the work tracker for whichever organisation the team belongs to. It holds no organisation of its own: each call takes the
/// team and goes to <c>team.Ado.Org</c> through the factory (ADR 0018).
/// </summary>
public sealed class RoutedWorkItemReader(IAdoClientFactory factory) : IWorkItemReader, IIterationReader, IBacklogReader
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

/// <summary>Writes an approved change to the organisation of the team it belongs to.</summary>
public sealed class RoutedWorkItemWriter(IAdoClientFactory factory) : IWorkItemWriter
{
    /// <inheritdoc />
    public Task<Result<WorkItemRevision>> ApplyAsync(ApprovedChange change, AdoSettings target, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(target);
        return factory.WriterFor(target.Org).ApplyAsync(change, ct);
    }
}

/// <summary>Removes the agent's comments in the team's project, in the team's organisation.</summary>
public sealed class RoutedDemoReset(IAdoClientFactory factory) : IDemoAdoReset
{
    /// <inheritdoc />
    public Task<int> RemoveAgentCommentsAsync(AdoSettings ado, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(ado);
        return new AdoDemoReset(factory.For(ado.Org)).RemoveAgentCommentsAsync(ado.Project, ct);
    }
}

/// <summary>Reads where a work item lives, so a write aimed at another team's item can be refused (<see cref="WorkItemScope"/>).</summary>
public sealed class AdoWorkItemScopeReader(IAdoClientFactory factory) : IWorkItemScopeReader
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
