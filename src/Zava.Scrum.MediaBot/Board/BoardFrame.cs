using System.Runtime.InteropServices;
using SkiaSharp;

namespace Zava.Scrum.MediaBot.Board;

/// <summary>An owned, opaque board frame; no native resources survive a render call.</summary>
public sealed class BoardFrame
{
    private readonly byte[] _rgb24;

    internal BoardFrame(byte[] rgb24)
    {
        _rgb24 = rgb24;
    }

    /// <summary>The fixed canvas width.</summary>
    public int Width => BoardRenderer.Width;
    /// <summary>The fixed canvas height.</summary>
    public int Height => BoardRenderer.Height;
    /// <summary>The packed RGB24 row length in bytes.</summary>
    public int Stride => Width * 3;
    /// <summary>Top-down RGB bytes, with no alpha or row padding; transport must retain/copy them for its buffer lifetime.</summary>
    public ReadOnlyMemory<byte> Rgb24 => _rgb24;

    /// <summary>Encodes the same pixels as a lossless PNG in memory, without writing a file.</summary>
    public byte[] ToPng()
    {
        var rgba = new byte[Width * Height * 4];
        for (var pixel = 0; pixel < Width * Height; pixel++)
        {
            _rgb24.AsSpan(pixel * 3, 3).CopyTo(rgba.AsSpan(pixel * 4, 3));
            rgba[(pixel * 4) + 3] = 255;
        }

        using var bitmap = new SKBitmap(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Opaque));
        Marshal.Copy(rgba, 0, bitmap.GetPixels(), rgba.Length);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }
}
