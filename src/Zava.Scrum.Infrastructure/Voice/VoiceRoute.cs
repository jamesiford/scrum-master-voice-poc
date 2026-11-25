namespace Zava.Scrum.Infrastructure.Voice;

/// <summary>
/// Picks the hosted agent endpoint for one standup: the stable agent, or the canary agent for a share of sessions in the teams allowed to canary
/// (ADR 0021). The choice is a pure function of the team and the session id, so every reconnect and the pre-warm reach the same agent.
/// </summary>
public static class VoiceRoute
{
    /// <summary>Chooses the endpoint.</summary>
    /// <param name="options">The voice settings.</param>
    /// <param name="teamId">The team the session belongs to.</param>
    /// <param name="sessionId">The session id.</param>
    /// <returns>The hosted <c>invocations_ws</c> endpoint, or an empty string for the direct route.</returns>
    public static string EndpointFor(VoiceLiveOptions options, string? teamId, Guid? sessionId)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.RelayEndpoint))
        {
            return string.Empty;
        }

        var eligible = options.CanaryTeams.Length == 0 || options.CanaryTeams.Contains(teamId, StringComparer.OrdinalIgnoreCase);
        var useCanary = !string.IsNullOrWhiteSpace(options.CanaryEndpoint) && options.CanaryPercent > 0 && eligible && sessionId is not null && Bucket(sessionId.Value) < options.CanaryPercent;
        return useCanary ? options.CanaryEndpoint : options.RelayEndpoint;
    }

    // The first four bytes of the id are random for a version 4 GUID, so the buckets are even and stable for a given session.
    private static int Bucket(Guid id) => (int)(BitConverter.ToUInt32(id.ToByteArray(), 0) % 100);
}
