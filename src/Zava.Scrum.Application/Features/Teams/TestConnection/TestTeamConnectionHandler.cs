using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Teams.TestConnection;

/// <summary>
/// Runs the tester. The tenant step is added here, from the same rule registration uses, so a test and a registration never disagree.
/// </summary>
internal sealed class TestTeamConnectionHandler(ITeamRepository teams, IAdoConnectionTester tester, IIdentityPolicy identity) : IRequestHandler<TestTeamConnectionQuery, Result<ConnectionReport>>
{
    public async Task<Result<ConnectionReport>> Handle(TestTeamConnectionQuery request, CancellationToken ct)
    {
        var ado = request.Ado;
        if (request.TeamId is not null)
        {
            var team = await teams.GetAsync(request.TeamId, ct);
            if (team is null)
            {
                return Result<ConnectionReport>.Failure(TeamErrors.NotFound(request.TeamId));
            }

            ado = team.Ado;
        }

        if (ado is null)
        {
            return Result<ConnectionReport>.Failure(Error.Invalid("team.ado_incomplete", "Give a team id or a connection to test."));
        }

        var org = AdoOrganisation.Normalise(ado.Org);
        if (org is null)
        {
            return Result<ConnectionReport>.Success(new ConnectionReport([new ConnectionStep("organisation", false, TeamErrors.BadOrganisation.Message)]));
        }

        var tenant = await tester.GetOrganisationTenantAsync(org, ct);
        if (tenant is not null && tenant != identity.TenantId)
        {
            return Result<ConnectionReport>.Success(new ConnectionReport([new ConnectionStep("tenant", false, TeamErrors.OtherTenant(org).Message)]));
        }

        return Result<ConnectionReport>.Success(await tester.TestAsync(ado with { Org = org }, ct));
    }
}
