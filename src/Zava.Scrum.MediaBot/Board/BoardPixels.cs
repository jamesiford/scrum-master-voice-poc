namespace Zava.Scrum.MediaBot.Board;

/// <summary>Converts opaque Skia RGBA rows to tightly packed, top-down RGB24 for the media SDK.</summary>
public static class BoardPixels
{
    /// <summary>Drops alpha and row padding without swapping red and blue or reversing rows.</summary>
    public static byte[] ToRgb24(ReadOnlySpan<byte> rgba, int width, int height, int rowBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (rowBytes < checked(width * 4) || rgba.Length < checked(rowBytes * height))
        {
            throw new ArgumentException("RGBA input must contain every complete row.", nameof(rgba));
        }

        var rgb = new byte[checked(width * height * 3)];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                rgba.Slice((y * rowBytes) + (x * 4), 3).CopyTo(rgb.AsSpan(((y * width) + x) * 3, 3));
            }
        }

        return rgb;
    }
}
