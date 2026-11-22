using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.FollowUps.SendFollowUps;

/// <summary>Plans and sends the follow-up, absent and escalation cards for an ended standup. Idempotent per session.</summary>
/// <param name="SessionId">The standup.</param>
public sealed record SendFollowUpsCommand(Guid SessionId) : ICommand<int>;
