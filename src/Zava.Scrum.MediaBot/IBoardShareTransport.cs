namespace Zava.Scrum.MediaBot;

/// <summary>Negotiates and sends one native screen-sharing stream, independently of voice.</summary>
public interface IBoardShareTransport
{
    /// <summary>Gets whether the negotiated socket can send.</summary>
    bool Active { get; }

    /// <summary>Requests the sharer role for the current call.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The negotiation task.</returns>
    Task StartAsync(CancellationToken ct);

    /// <summary>Relinquishes the sharer role for the current call.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The negotiation task.</returns>
    Task StopAsync(CancellationToken ct);

    /// <summary>Sends a single raw 1280 by 720 RGB24 frame.</summary>
    /// <param name="rgb">The rendered frame.</param>
    void Send(byte[] rgb);
}
