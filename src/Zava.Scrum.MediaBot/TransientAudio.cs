namespace Zava.Scrum.MediaBot;

/// <summary>PCM frame operations that never use storage and never resample.</summary>
public static class TransientAudio
{
    /// <summary>Computes mean absolute PCM16 amplitude to select the audible source from an unmixed batch.</summary>
    /// <param name="pcm">A PCM16 little-endian frame.</param>
    /// <returns>The mean amplitude.</returns>
    public static int Amplitude(ReadOnlySpan<byte> pcm)
    {
        long total = 0;
        for (var i = 0; i + 1 < pcm.Length; i += 2)
        {
            total += Math.Abs((int)System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(pcm[i..]));
        }

        return pcm.Length < 2 ? 0 : (int)(total / (pcm.Length / 2));
    }
}
