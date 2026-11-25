namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>What the deployment knows about who may act: the tenant it belongs to and its configured administrators.</summary>
public interface IIdentityPolicy
{
    /// <summary>Gets the Entra tenant this deployment belongs to. An identity from any other tenant is refused.</summary>
    Guid TenantId { get; }

    /// <summary>Checks whether an Entra object id is a configured administrator (the Teams equivalent of the admin role).</summary>
    /// <param name="objectId">The Entra object id.</param>
    /// <returns><see langword="true"/> when it is configured as an administrator.</returns>
    bool IsAdmin(Guid objectId);
}
