using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Standups.ControlStandup;

/// <summary>Steers a running standup from the portal: mute, unmute, skip, takeover, release or end.</summary>
/// <param name="SessionId">The standup.</param>
/// <param name="Action">The action.</param>
public sealed record ControlStandupCommand(Guid SessionId, string Action) : ICommand;
