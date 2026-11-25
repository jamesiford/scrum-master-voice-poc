using Zava.Scrum.Domain.Identity;

namespace Zava.Scrum.Infrastructure.Tests;

/// <summary>Verified identities for adapter tests.</summary>
internal static class Actors
{
    public static ActorIdentity Dana(ActorSource source) =>
        ActorIdentity.Create("22222222-2222-2222-2222-222222222222", "11111111-1111-1111-1111-111111111111", "Dana", null, source, source == ActorSource.Portal ? "entra-bearer" : "bot-service-token").Value;
}
