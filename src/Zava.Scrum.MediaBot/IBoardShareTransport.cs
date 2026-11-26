namespace Zava.Scrum.MediaBot;

/// <summary>Negotiates and sends one native screen-sharing stream, independently of voice.</summary>
public interface IBoardShareTransport
{
    /// <summary>Requests the sharer role for the current call.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The negotiation task.</returns>
    Task StartAsync(CancellationToken ct);

    /// <summary>Relinquishes the sharer role for the current call.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The negotiation task.</returns>
    Task StopAsync(CancellationToken ct);

    /// <summary>Submits a single raw 1280 by 720 RGB24 frame when negotiation allows it.</summary>
    /// <param name="rgb">The rendered frame.</param>
    /// <returns>True after SDK submission; false while the socket is not negotiated.</returns>
    bool TrySend(byte[] rgb);
}
