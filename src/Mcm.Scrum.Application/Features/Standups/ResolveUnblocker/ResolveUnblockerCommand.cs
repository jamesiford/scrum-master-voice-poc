using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Standups.ResolveUnblocker;

/// <summary>
/// Reads a blocked member's answer to "Who can help unblock this?" and, when it names exactly one other roster member, stores that
/// member on the member's open blocker (M9-R2). Returns the unblocker's member id, or <see langword="null"/> when nobody was named.
/// </summary>
/// <param name="SessionId">Session id.</param>
/// <param name="MemberId">The blocked member.</param>
/// <param name="Answer">What they said.</param>
public sealed record ResolveUnblockerCommand(Guid SessionId, string MemberId, string Answer) : ICommand<string?>;
