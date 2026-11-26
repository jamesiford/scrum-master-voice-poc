using System.Buffers.Binary;
using Zava.Scrum.MediaBot;

namespace Zava.Scrum.MediaBot.Tests;

public sealed class TransientAudioTests
{
    [Fact]
    public void Amplitude_NegativeAndPositiveSamples_MeasuresAudibleMagnitude()
    {
        var pcm = new byte[4];
        BinaryPrimitives.WriteInt16LittleEndian(pcm, short.MinValue);
        BinaryPrimitives.WriteInt16LittleEndian(pcm.AsSpan(2), short.MaxValue);
        Assert.Equal(32767, TransientAudio.Amplitude(pcm));
        Assert.Equal(0, TransientAudio.Amplitude(new byte[960]));
    }
}
