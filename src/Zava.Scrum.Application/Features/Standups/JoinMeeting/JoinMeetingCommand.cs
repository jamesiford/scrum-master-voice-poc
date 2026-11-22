using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Standups.JoinMeeting;

/// <summary>Asks the bridge to put the agent in the session's Teams meeting. The session must already be started with a join URL.</summary>
/// <param name="SessionId">The standup.</param>
/// <param name="BridgeToken">Token for the call page.</param>
/// <param name="ApiBase">Public API address.</param>
public sealed record JoinMeetingCommand(Guid SessionId, string BridgeToken, string ApiBase) : ICommand;
