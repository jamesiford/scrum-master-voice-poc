using Mcm.Scrum.Application.Abstractions.Ports;

namespace Mcm.Scrum.Api.Voice;

/// <summary>Gives the application layer the bridge token and public address without exposing the token service (SPEC 8).</summary>
public sealed class BridgeAccess(BridgeTokenService tokens, IConfiguration configuration) : IBridgeAccess
{
    /// <inheritdoc />
    public string ApiBase => (configuration["Bridge:ApiPublicUrl"] ?? "http://localhost:5080").TrimEnd('/');

    /// <inheritdoc />
    public string Issue(Guid sessionId) => tokens.Issue(sessionId);
}
