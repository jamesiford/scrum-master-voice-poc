using Zava.Scrum.Domain.Identity;

namespace Zava.Scrum.Domain.Tests;

/// <summary>Verified identities for tests. Display names carry no authority; the object id and tenant do.</summary>
internal static class Actors
{
    public static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static ActorIdentity Dana { get; } = Make("22222222-2222-2222-2222-222222222222", "Dana", ActorSource.Teams);

    public static ActorIdentity DanaInPortal { get; } = Make("22222222-2222-2222-2222-222222222222", "Dana", ActorSource.Portal);

    public static ActorIdentity Make(string objectId, string name, ActorSource source, string? tenant = null) =>
        ActorIdentity.Create(objectId, tenant ?? Tenant.ToString(), name, null, source, source == ActorSource.Portal ? "entra-bearer" : "bot-service-token").Value;
}
