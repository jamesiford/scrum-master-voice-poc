namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Credentials and address the meeting bridge needs to reach this API's websocket.</summary>
public interface IBridgeAccess
{
    /// <summary>Gets the public address of this API.</summary>
    string ApiBase { get; }

    /// <summary>Issues the short-lived token for a session's websocket.</summary>
    /// <param name="sessionId">The session.</param>
    /// <returns>The token.</returns>
    string Issue(Guid sessionId);
}
