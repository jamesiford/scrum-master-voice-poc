using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Identity;

namespace Zava.Scrum.Application.Features.Identity;

/// <summary>
/// Who is deciding and how that was proven. Presentation adapters build it from a validated token or a Bot Service activity;
/// the handler checks it with <see cref="ApprovalAuthority"/> before anything changes.
/// </summary>
/// <param name="Actor">The verified identity.</param>
/// <param name="Channel">Where the decision came from.</param>
/// <param name="Authentication">The claims that proved the sign-in (no tokens).</param>
/// <param name="Roles">App roles from the validated portal token; empty for Teams.</param>
public sealed record DecisionContext(ActorIdentity Actor, ApprovalChannel Channel, AuthenticationContext Authentication, IReadOnlyCollection<string> Roles)
{
    /// <summary>Creates a Teams decision (no token roles; authority comes from the roster or the administrator list).</summary>
    /// <param name="actor">The identity from the activity.</param>
    /// <returns>The context.</returns>
    public static DecisionContext FromTeams(ActorIdentity actor) =>
        new(actor, ApprovalChannel.Teams, AuthenticationContext.Of(actor.AuthenticationMethod), []);
}
