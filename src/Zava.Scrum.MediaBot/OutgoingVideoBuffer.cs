using System.Runtime.InteropServices;
using Microsoft.Skype.Bots.Media;

namespace Zava.Scrum.MediaBot;

/// <summary>Retains a raw board frame until the native encoder releases its buffer.</summary>
public sealed class OutgoingVideoBuffer : VideoMediaBuffer
{
    private IntPtr _data;

    /// <summary>Copies the frame; the SDK owns its lifetime after Send succeeds.</summary>
    /// <param name="rgb">RGB24 pixels, row-major, at 1280 by 720.</param>
    /// <param name="format">The negotiated raw source format.</param>
    public OutgoingVideoBuffer(byte[] rgb, VideoFormat format)
    {
        ArgumentNullException.ThrowIfNull(rgb);
        if (rgb.Length != 1280 * 720 * 3 || format.Width != 1280 || format.Height != 720 || format.VideoColorFormat != VideoColorFormat.Rgb24)
        {
            throw new ArgumentException("The board requires 1280 by 720 RGB24.", nameof(rgb));
        }

        _data = Marshal.AllocHGlobal(rgb.Length);
        Marshal.Copy(rgb, 0, _data, rgb.Length);
        Data = _data;
        Length = rgb.Length;
        Stride = 1280 * 3;
        VideoFormat = format;
        Timestamp = MediaPlatform.GetCurrentTimestamp();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        var data = Interlocked.Exchange(ref _data, IntPtr.Zero);
        if (data != IntPtr.Zero) { Marshal.FreeHGlobal(data); }
    }
}
