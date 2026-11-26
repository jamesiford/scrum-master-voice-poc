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
    private VideoFormat? _format;

    /// <summary>Subscribes to the socket before requesting a sharing role.</summary>
    /// <param name="call">The authenticated group call.</param>
    /// <param name="socket">Its VBSS send socket.</param>
    /// <param name="logger">Metadata-only diagnostics.</param>
    public MediaBoardTransport(ICall call, IVideoSocket socket, ILogger logger)
    {
        _call = call;
        _socket = socket;
        _logger = logger;
        _socket.VideoSendStatusChanged += OnSendStatus;
    }

    /// <inheritdoc />
    public bool Active => Volatile.Read(ref _format) is not null;

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
    public void Send(byte[] rgb)
    {
        var format = Volatile.Read(ref _format);
        if (format is null) { return; }
        var buffer = new OutgoingVideoBuffer(rgb, format);
        try { _socket.Send(buffer); }
        catch { buffer.Dispose(); throw; }
    }

    /// <inheritdoc />
    public void Dispose() => _socket.VideoSendStatusChanged -= OnSendStatus;

    private void OnSendStatus(object? sender, VideoSendStatusChangedEventArgs args)
    {
        var format = args.PreferredVideoSourceFormat;
        var valid = args.MediaSendStatus == MediaSendStatus.Active && format is not null
            && format.Width == 1280 && format.Height == 720 && format.VideoColorFormat == VideoColorFormat.Rgb24;
        Volatile.Write(ref _format, valid ? format : null);
        if (args.MediaSendStatus == MediaSendStatus.Active && !valid)
        {
            _logger.LogError("VBSS negotiated an unsupported source format; board disabled for call {CallId}", _call.Id);
        }
    }
}
