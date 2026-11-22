using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Teams.SetRecapChannel;

/// <summary>Records the Teams channel the standup recap is posted to. The Teams bot sends it when it is installed in a team channel (M9-R5).</summary>
/// <param name="TeamId">Team id.</param>
/// <param name="ConversationId">Teams conversation id of the channel.</param>
public sealed record SetRecapChannelCommand(string TeamId, string ConversationId) : ICommand;
