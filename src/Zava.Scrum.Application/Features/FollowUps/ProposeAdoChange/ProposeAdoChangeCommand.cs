using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Application.Features.FollowUps.ProposeAdoChange;

/// <summary>Creates a pending follow-up with one proposed change. Nothing is written until it is approved.</summary>
/// <param name="SessionId">The standup it came from.</param>
/// <param name="MemberId">The member it concerns.</param>
/// <param name="WorkItemId">Work item to change; must be in the current sprint.</param>
/// <param name="Op">Kind of change.</param>
/// <param name="Value">Comment, state or tag instruction.</param>
/// <param name="Rationale">Why.</param>
public sealed record ProposeAdoChangeCommand(Guid SessionId, string MemberId, int WorkItemId, ChangeOp Op, string Value, string Rationale) : ICommand<Guid>;
