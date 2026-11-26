using System.Runtime.InteropServices;
using Microsoft.Skype.Bots.Media;

namespace Zava.Scrum.MediaBot;

/// <summary>Owns exactly one unmanaged PCM frame until the media platform disposes it.</summary>
public sealed class OutgoingAudioBuffer : AudioMediaBuffer
{
    private IntPtr _data;

    /// <summary>Copies a 20 ms PCM24K frame into platform-owned memory.</summary>
    /// <param name="pcm">960 bytes of PCM16 mono audio.</param>
    public OutgoingAudioBuffer(byte[] pcm)
    {
        ArgumentNullException.ThrowIfNull(pcm);
        if (pcm.Length != 960)
        {
            throw new ArgumentException("PCM24K frames must be 960 bytes.", nameof(pcm));
        }

        _data = Marshal.AllocHGlobal(pcm.Length);
        Marshal.Copy(pcm, 0, _data, pcm.Length);
        Data = _data;
        Length = pcm.Length;
        AudioFormat = AudioFormat.Pcm24K;
        Timestamp = MediaPlatform.GetCurrentTimestamp();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        var data = Interlocked.Exchange(ref _data, IntPtr.Zero);
        if (data != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(data);
        }
    }
}
