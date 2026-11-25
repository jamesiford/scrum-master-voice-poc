using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Teams;

/// <summary>
/// The checks every registration and edit share (DRY): a valid organisation URL, and an organisation that belongs to this deployment's Entra
/// tenant, because a managed identity from another tenant cannot be added to it (ADR 0018).
/// </summary>
internal sealed class TeamRegistration(IAdoConnectionTester tester, IIdentityPolicy identity)
{
    /// <summary>Validates and normalises the Azure DevOps connection.</summary>
    /// <param name="ado">The connection as entered.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The connection with a normalised organisation URL, or the reason it was refused.</returns>
    public async Task<Result<AdoSettings>> ValidateAsync(AdoSettings ado, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(ado);
        var org = AdoOrganisation.Normalise(ado.Org);
        if (org is null)
        {
            return Result<AdoSettings>.Failure(TeamErrors.BadOrganisation);
        }

        if (string.IsNullOrWhiteSpace(ado.Project) || string.IsNullOrWhiteSpace(ado.Team) || string.IsNullOrWhiteSpace(ado.AreaPath))
        {
            return Result<AdoSettings>.Failure(Error.Invalid("team.ado_incomplete", "The project, Azure DevOps team and area path are all required."));
        }

        var tenant = await tester.GetOrganisationTenantAsync(org, ct);
        if (tenant is null)
        {
            return Result<AdoSettings>.Failure(TeamErrors.TenantUnknown(org));
        }

        return tenant != identity.TenantId
            ? Result<AdoSettings>.Failure(TeamErrors.OtherTenant(org))
            : Result<AdoSettings>.Success(ado with { Org = org });
    }
}
