using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Teams.AdoDiscovery;

/// <summary>Lists the projects of an organisation, for the onboarding wizard's picker. The organisation must pass the same tenant rule as a registration.</summary>
/// <param name="Organisation">The organisation URL.</param>
public sealed record ListAdoProjectsQuery(string Organisation) : IQuery<IReadOnlyList<AdoProjectRef>>;

/// <summary>Lists the teams of a project, for the onboarding wizard's picker.</summary>
/// <param name="Organisation">The organisation URL.</param>
/// <param name="Project">The project.</param>
public sealed record ListAdoTeamsQuery(string Organisation, string Project) : IQuery<IReadOnlyList<AdoTeamRef>>;

internal sealed class ListAdoProjectsHandler(IAdoConnectionTester tester, IIdentityPolicy identity) : IRequestHandler<ListAdoProjectsQuery, Result<IReadOnlyList<AdoProjectRef>>>
{
    public async Task<Result<IReadOnlyList<AdoProjectRef>>> Handle(ListAdoProjectsQuery request, CancellationToken ct)
    {
        var org = AdoOrganisation.Normalise(request.Organisation);
        if (org is null)
        {
            return Result<IReadOnlyList<AdoProjectRef>>.Failure(Teams.TeamErrors.BadOrganisation);
        }

        if (await tester.GetOrganisationTenantAsync(org, ct) is { } tenant && tenant != identity.TenantId)
        {
            return Result<IReadOnlyList<AdoProjectRef>>.Failure(Teams.TeamErrors.OtherTenant(org));
        }

        return Result<IReadOnlyList<AdoProjectRef>>.Success(await tester.ListProjectsAsync(org, ct));
    }
}

internal sealed class ListAdoTeamsHandler(IAdoConnectionTester tester, IIdentityPolicy identity) : IRequestHandler<ListAdoTeamsQuery, Result<IReadOnlyList<AdoTeamRef>>>
{
    public async Task<Result<IReadOnlyList<AdoTeamRef>>> Handle(ListAdoTeamsQuery request, CancellationToken ct)
    {
        var org = AdoOrganisation.Normalise(request.Organisation);
        if (org is null)
        {
            return Result<IReadOnlyList<AdoTeamRef>>.Failure(Teams.TeamErrors.BadOrganisation);
        }

        if (await tester.GetOrganisationTenantAsync(org, ct) is { } tenant && tenant != identity.TenantId)
        {
            return Result<IReadOnlyList<AdoTeamRef>>.Failure(Teams.TeamErrors.OtherTenant(org));
        }

        return Result<IReadOnlyList<AdoTeamRef>>.Success(await tester.ListTeamsAsync(org, request.Project, ct));
    }
}
