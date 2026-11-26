using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Models;
using Microsoft.Skype.Bots.Media;

namespace Zava.Scrum.MediaBot;

/// <summary>The real Graph sharing-role and SDK VBSS adapter; never a camera-video substitute.</summary>
public sealed class MediaBoardTransport : IBoardShareTransport, IDisposable
{
    private readonly ICall _call;
    private readonly IVideoSocket _socket;
    private readonly ILogger _logger;
    private readonly string _teamId;
    private readonly Guid _sessionId;
    private VideoFormat? _format;
    private string? _sourceFailure;

    /// <summary>Subscribes to the socket before requesting a sharing role.</summary>
    /// <param name="call">The authenticated group call.</param>
    /// <param name="socket">Its VBSS send socket.</param>
    /// <param name="logger">Metadata-only diagnostics.</param>
    /// <param name="teamId">The authenticated team owning the call.</param>
    /// <param name="sessionId">The owning ceremony session.</param>
    public MediaBoardTransport(ICall call, IVideoSocket socket, ILogger logger, string teamId, Guid sessionId)
    {
        _call = call;
        _socket = socket;
        _logger = logger;
        _teamId = teamId;
        _sessionId = sessionId;
        _socket.VideoSendStatusChanged += OnSendStatus;
    }

    /// <summary>Declares raw formats at one native resolution, including the mandatory 1.875 fps.</summary>
    /// <returns>The SDK VBSS settings.</returns>
    public static VideoSocketSettings SocketSettings() => new()
    {
        MediaType = MediaType.Vbss,
        StreamDirections = StreamDirection.Sendonly,
        SupportedSendVideoFormats =
        [
            VideoFormat.Rgb24_1280x720_1_875Fps,
            VideoFormat.Rgb24_1280x720_3_75Fps,
            VideoFormat.Rgb24_1280x720_7_5Fps,
            VideoFormat.Rgb24_1280x720_15Fps,
            VideoFormat.Rgb24_1280x720_30Fps,
        ],
    };

    /// <inheritdoc />
    public Task StartAsync(CancellationToken ct) => _call.ChangeScreenSharingRoleAsync(ScreenSharingRole.Sharer, ct);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken ct) => _call.Resource.State == CallState.Terminated
        ? Task.CompletedTask : _call.ChangeScreenSharingRoleAsync(ScreenSharingRole.Viewer, ct);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The socket reports an unsupported source format.</exception>
    public bool TrySend(byte[] rgb)
    {
        var failure = Volatile.Read(ref _sourceFailure);
        if (failure is not null) { throw new InvalidOperationException(failure); }
        var format = Volatile.Read(ref _format);
        if (format is null) { return false; }
        var buffer = new OutgoingVideoBuffer(rgb, format);
        try { _socket.Send(buffer); }
        catch { buffer.Dispose(); throw; }
        return true;
    }

    /// <inheritdoc />
    public void Dispose() => _socket.VideoSendStatusChanged -= OnSendStatus;

    private void OnSendStatus(object? sender, VideoSendStatusChangedEventArgs args)
    {
        var format = args.PreferredVideoSourceFormat;
        var valid = args.MediaSendStatus == MediaSendStatus.Active && format is not null
            && format.Width == 1280 && format.Height == 720 && format.VideoColorFormat == VideoColorFormat.Rgb24;
        Volatile.Write(ref _format, valid ? format : null);
        Volatile.Write(ref _sourceFailure, args.MediaSendStatus == MediaSendStatus.Active && !valid
            ? "VBSS negotiated an unsupported source format." : null);
        if (args.MediaSendStatus == MediaSendStatus.Active && !valid)
        {
            _logger.LogError("VBSS negotiated an unsupported source format for {TeamId}/{SessionId}, call {CallId}",
                _teamId, _sessionId, _call.Id);
        }
    }
}
