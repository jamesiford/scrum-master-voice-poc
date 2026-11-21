using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Abstractions.Ports;

/// <summary>Everything the bridge needs to join a Teams meeting.</summary>
/// <param name="SessionId">The standup.</param>
/// <param name="JoinUrl">Teams meeting link.</param>
/// <param name="BridgeToken">Short-lived token the call page presents to the gateway.</param>
/// <param name="ApiBase">Public address of this API, for the call page's websocket.</param>
/// <param name="DisplayName">Name shown in the meeting roster.</param>
public sealed record MeetingJoinRequest(Guid SessionId, string JoinUrl, string BridgeToken, string ApiBase, string DisplayName);

/// <summary>Controls the meeting bridge (headless browser plus ACS) that puts the agent in a Teams meeting.</summary>
public interface IMeetingBridge
{
    /// <summary>Asks the bridge to join a meeting. Returns once the join has been started, not when the agent is admitted.</summary>
    /// <param name="request">The join request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success, or an <see cref="ErrorType.ExternalRejected"/> failure.</returns>
    Task<Result> JoinAsync(MeetingJoinRequest request, CancellationToken ct);

    /// <summary>Asks the bridge to leave a meeting. Safe to call when not joined.</summary>
    /// <param name="sessionId">The standup.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task LeaveAsync(Guid sessionId, CancellationToken ct);
}