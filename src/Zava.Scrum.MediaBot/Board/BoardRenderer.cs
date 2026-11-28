using System.Runtime.InteropServices;
using System.Text.Json;
using SkiaSharp;

namespace Zava.Scrum.MediaBot.Board;

/// <summary>Browser-free shared board with larger standup text and the same product facts as board.ts.</summary>
public sealed class BoardRenderer : IDisposable
{
    /// <summary>The existing browser canvas width.</summary>
    public const int Width = 1280;
    /// <summary>The existing browser canvas height.</summary>
    public const int Height = 720;
    /// <summary>The standup's minimum font size before receiver downscaling.</summary>
    public const int BodyTextSize = 24;

    private readonly SKTypeface _regular = FindFont(SKFontStyle.Normal);
    private readonly SKTypeface _semibold = FindFont(new SKFontStyle(600, 5, SKFontStyleSlant.Upright));
    private readonly SKTypeface _numbers = FindFont(SKFontStyle.Normal, "Consolas");
    private bool _disposed;

    /// <summary>Renders a gateway board message directly to packed, top-down RGB24.</summary>
    public byte[] RenderRgb24(JsonElement board, string clockText) =>
        Render(board.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ? null : BoardState.FromJson(board), clockText).Rgb24.ToArray();

    /// <summary>Renders a gateway board message to a lossless PNG in memory for golden comparisons.</summary>
    public byte[] RenderPng(JsonElement board, string clockText) =>
        Render(board.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ? null : BoardState.FromJson(board), clockText).ToPng();

    /// <summary>Paints one immutable frame; null state paints the waiting screen. Clock formatting belongs to the caller.</summary>
    public BoardFrame Render(BoardState? state, string clockText)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(clockText);
        using var bitmap = new SKBitmap(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Opaque));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColor.Parse("#0f172a"));
        Text(canvas, clockText, 1080, 36, BodyTextSize, "#94a3b8");
        if (state is RetroBoardState retro)
        {
            PaintRetro(canvas, retro);
        }
        else
        {
            PaintStandup(canvas, state as StandupBoardState);
        }

        var rgba = new byte[bitmap.ByteCount];
        Marshal.Copy(bitmap.GetPixels(), rgba, 0, rgba.Length);
        return new BoardFrame(BoardPixels.ToRgb24(rgba, Width, Height, bitmap.RowBytes));
    }

    /// <summary>Releases the font handles; frames already returned remain valid.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _regular.Dispose();
        _semibold.Dispose();
        _numbers.Dispose();
    }

    private static SKTypeface FindFont(SKFontStyle style, string family = "Segoe UI")
    {
        // The native media host is Windows. Fail visibly rather than silently changing the board's typography.
        return SKFontManager.Default.MatchFamily(family, style)
            ?? throw new InvalidOperationException($"The shared board requires the Windows {family} font.");
    }

    private void PaintStandup(SKCanvas canvas, StandupBoardState? state)
    {
        Text(canvas, !string.IsNullOrEmpty(state?.Speaker) ? $"{state.Speaker}'s items" : "Standup agenda: flagged items", 48, 64, 34, "#f8fafc", true);
        if (state is null)
        {
            Text(canvas, "Preparing the sprint board...", 48, 112, BodyTextSize, "#cbd5e1");
            return;
        }

        Text(canvas, $"{state.Sprint} · {state.DaysLeft} days left", 48, 104, 32, "#e2e8f0", numbers: true);
        if (!string.IsNullOrEmpty(state.Goal))
        {
            Text(canvas, $"Goal: {Clip(state.Goal, 70)}", 48, 140, BodyTextSize, "#e2e8f0");
        }
        if (state.Ended)
        {
            Text(canvas, "Standup complete.", 48, 218, 32, "#4ade80", true);
            Text(canvas, state.Recording == false
                ? "Meeting-derived data is not saved."
                : "Follow-ups are in Teams for approval.", 48, 264, BodyTextSize, "#e2e8f0");
        }
        else
        {
            DrawItems(canvas, state);
        }

        DrawTeam(canvas, state);
    }

    private void DrawItems(SKCanvas canvas, StandupBoardState state)
    {
        if (state.Items.Count == 0)
        {
            Text(canvas, "No items assigned.", 48, 218, BodyTextSize, "#cbd5e1");
            return;
        }

        foreach (var (item, i) in state.Items.Take(6).Select((item, index) => (item, index)))
        {
            var y = 164 + (i * 84);
            Rect(canvas, 48, y, 840, 76, item.Covered ? "#14532d" : "#1e293b");
            if (item.Id == state.Focus)
            {
                Rect(canvas, 48, y, 840, 76, "#38bdf8", stroke: true);
            }

            Rect(canvas, 48, y, 8, 76, item.Covered ? "#4ade80" : item.Flags.Count > 0 ? "#fbbf24" : "#64748b");
            Text(canvas, $"{(item.Covered ? "✓ " : string.Empty)}#{item.Id}  {Clip(item.Title, 52)}", 72, y + 30, 26, "#f8fafc", true);
            var flags = item.Flags.Count > 0 ? $" · {string.Join(", ", item.Flags)}" : string.Empty;
            var owner = !string.IsNullOrEmpty(item.Owner) && string.IsNullOrEmpty(state.Speaker) ? $" · {item.Owner}" : string.Empty;
            Text(canvas, $"{item.Type} · {item.State}{flags}{owner}", 72, y + 62, BodyTextSize, "#cbd5e1");
        }
    }

    private void DrawTeam(SKCanvas canvas, StandupBoardState state)
    {
        var compact = state.Team.Count > 5;
        Text(canvas, "Coverage", 920, compact ? 160 : 190, compact ? 22 : 28, "#f8fafc", true);
        var spacing = compact ? 56 : Math.Min(118, 420f / Math.Max(1, state.Team.Count));
        foreach (var (member, i) in state.Team.Select((member, index) => (member, index)))
        {
            var y = (compact ? 200 : 238) + (i * spacing);
            var status = string.IsNullOrEmpty(member.Status) ? string.Empty : $" · {member.Status}";
            if (compact)
            {
                Text(canvas, $"{member.Name} {member.Covered}/{member.Total}{status}", 920, y, 20,
                    member.Name == state.Speaker ? "#38bdf8" : "#e2e8f0");
            }
            else
            {
                Text(canvas, member.Name, 920, y, 26, member.Name == state.Speaker ? "#38bdf8" : "#e2e8f0", true);
                Text(canvas, $"Items: {member.Covered}/{member.Total}{status}", 920, y + 34, BodyTextSize, "#cbd5e1");
            }

            var barY = y + (compact ? 10 : 48);
            Rect(canvas, 920, barY, 308, 10, "#334155");
            Rect(canvas, 920, barY, member.Total != 0 ? 308f * member.Covered / member.Total : 0, 10, "#4ade80");
        }

        if (!compact && !state.Ended && state.Focus is { } focus && state.Items.Take(6).Any(item => item.Id == focus))
        {
            Text(canvas, $"Current item #{focus}", 920, 678, BodyTextSize, "#38bdf8", true);
        }
    }

    private void PaintRetro(SKCanvas canvas, RetroBoardState state)
    {
        Text(canvas, "Sprint retrospective", 48, 70, 34, "#e2e8f0", true);
        var turn = string.IsNullOrEmpty(state.Speaker) ? string.Empty : $" · {state.Speaker} is sharing";
        Text(canvas, $"{state.Sprint} · {(state.Phase == "Actions" ? "Agreeing actions" : "Collecting notes")}{turn}", 48, 108, 20, "#94a3b8");
        if (state.Ended)
        {
            Text(canvas, state.Recording == false
                ? "Retrospective complete. Meeting-derived data is not saved."
                : "Retrospective complete. The action follow-up is in Teams for approval.", 48, 140, 24, "#4ade80", true);
        }

        DrawColumns(canvas, state);
        Text(canvas, state.Phase == "Actions" ? "Actions" : "Themes and actions appear after the round", 48, 520, 22, "#e2e8f0", true);
        if (state.Themes.Count > 0)
        {
            Text(canvas, $"Themes: {Clip(string.Join(", ", state.Themes.Select(theme => $"{theme.Name} ({theme.Count})")), 110)}", 48, 552, 18, "#94a3b8");
        }

        foreach (var (action, i) in state.Actions.Take(3).Select((action, index) => (action, index)))
        {
            Text(canvas, $"{action.Owner}: {Clip(action.Text, 100)}", 48, 590 + (i * 30), 18, "#4ade80");
        }
    }

    private void DrawColumns(SKCanvas canvas, RetroBoardState state)
    {
        string[] colors = ["#4ade80", "#fbbf24", "#38bdf8"];
        foreach (var (column, i) in state.Columns.Select((column, index) => (column, index)))
        {
            var x = 48 + (i * 400);
            Rect(canvas, x, 160, 380, 320, "#1e293b");
            Rect(canvas, x, 160, 380, 6, colors[i % colors.Length]);
            Text(canvas, $"{column.Label} ({column.Notes.Count})", x + 16, 200, 22, "#f8fafc", true);
            foreach (var (note, row) in column.Notes.TakeLast(5).Select((note, index) => (note, index)))
            {
                foreach (var (line, l) in Wrap(note.Text, 40).Take(2).Select((line, index) => (line, index)))
                {
                    Text(canvas, line, x + 16, 232 + (row * 48) + (l * 20), 17, "#e2e8f0");
                }
            }
        }
    }

    private void Text(SKCanvas canvas, string text, float x, float y, float size, string color, bool bold = false, bool numbers = false)
    {
        using var font = new SKFont(numbers ? _numbers : bold ? _semibold : _regular, size)
        {
            Edging = SKFontEdging.Antialias,
            Subpixel = true,
            Hinting = SKFontHinting.Normal,
            LinearMetrics = true,
        };
        using var paint = new SKPaint { Color = SKColor.Parse(color), IsAntialias = true };
        DrawTextWithFallback(canvas, text, x, y, font, paint, bold);
    }

    private static void DrawTextWithFallback(SKCanvas canvas, string text, float x, float y, SKFont font, SKPaint paint, bool bold)
    {
        var start = 0;
        var offset = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            if (font.GetGlyph(rune.Value) == 0)
            {
                var run = text[start..offset];
                x += DrawRun(canvas, run, x, y, font, paint);
                using var typeface = SKFontManager.Default.MatchCharacter("Segoe UI Symbol", rune.Value)
                    ?? throw new InvalidOperationException($"No Windows font contains U+{rune.Value:X}.");
                using var fallback = new SKFont(typeface, font.Size)
                {
                    Edging = font.Edging,
                    Subpixel = font.Subpixel,
                    Hinting = font.Hinting,
                    LinearMetrics = font.LinearMetrics,
                    Embolden = bold && typeface.FontWeight < 600,
                };
                var character = rune.ToString();
                x += DrawRun(canvas, character, x, y, fallback, paint);
                start = offset + rune.Utf16SequenceLength;
            }

            offset += rune.Utf16SequenceLength;
        }

        DrawRun(canvas, text[start..], x, y, font, paint);
    }

    private static float DrawRun(SKCanvas canvas, string text, float x, float y, SKFont font, SKPaint paint)
    {
        if (text.Length == 0)
        {
            return 0;
        }

        var glyphs = new ushort[font.CountGlyphs(text)];
        font.GetGlyphs(text, glyphs);
        var points = new SKPoint[glyphs.Length];
        font.GetGlyphPositions(glyphs, points);
        var kerning = font.Typeface.GetKerningPairAdjustments(glyphs);
        // Basic font kerning is retained. Chromium also shapes GPOS pairs/ligatures, so native text is not pixel-identical.
        var adjustment = 0f;
        for (var i = 1; i < points.Length && kerning is not null; i++)
        {
            adjustment += kerning[i - 1] * font.Size / font.Typeface.UnitsPerEm;
            points[i].X += adjustment;
        }

        using var blob = SKTextBlob.CreatePositioned(text, font, points);
        canvas.DrawText(blob, x, y, paint);
        return font.MeasureText(text) + adjustment;
    }

    private static void Rect(SKCanvas canvas, float x, float y, float width, float height, string color, bool stroke = false)
    {
        using var paint = new SKPaint
        {
            Color = SKColor.Parse(color),
            Style = stroke ? SKPaintStyle.Stroke : SKPaintStyle.Fill,
            StrokeWidth = 3,
            IsAntialias = true,
        };
        canvas.DrawRect(x, y, width, height, paint);
    }

    private static string Clip(string text, int max) => text.Length > max ? string.Concat(text.AsSpan(0, max - 1), "…") : text;

    private static IEnumerable<string> Wrap(string text, int width)
    {
        var line = string.Empty;
        foreach (var word in text.Split(' '))
        {
            var candidate = $"{line} {word}".Trim();
            if (candidate.Length > width)
            {
                yield return line;
                line = word;
            }
            else
            {
                line = candidate;
            }
        }

        if (line.Length > 0)
        {
            yield return line;
        }
    }
}
