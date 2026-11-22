namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Opens realtime voice sessions (Azure Voice Live).</summary>
public interface IVoiceSessionFactory
{
    /// <summary>Connects and configures a session.</summary>
    /// <param name="config">Session settings.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The open session.</returns>
    Task<IVoiceSession> CreateAsync(VoiceSessionConfig config, CancellationToken ct);
}
