using System.Security.Cryptography;
using System.Text.Json;
using SkiaSharp;
using Zava.Scrum.MediaBot.Board;

namespace Zava.Scrum.MediaBot.Tests;

public sealed class BoardRendererTests
{
    private const string Clock = "1:23:45 PM";
    private static readonly JsonSerializerOptions ReportOptions = new() { WriteIndented = true };

    [Fact]
    public void Render_AllBrowserStates_MatchesChromiumWithinDeclaredRasterTolerance()
    {
        var assets = AssetsPath();
        using var fixtures = JsonDocument.Parse(File.ReadAllText(Path.Combine(assets, "fixtures.json")));
        using var renderer = new BoardRenderer();
        var reports = new List<object>();
        var failures = new List<string>();
        var output = Path.Combine(assets, "native");
        Directory.CreateDirectory(output);
        foreach (var fixture in fixtures.RootElement.EnumerateArray())
        {
            var name = fixture.GetProperty("name").GetString()!;
            var state = fixture.GetProperty("state");
            var frame = renderer.Render(state.ValueKind == JsonValueKind.Null ? null : BoardState.FromJson(state), Clock);
            var png = frame.ToPng();
            File.WriteAllBytes(Path.Combine(output, $"{name}.png"), png);
            using var baseline = SKBitmap.Decode(Path.Combine(assets, "chromium", $"{name}.png"));
            using var actual = SKBitmap.Decode(png);
            Assert.Equal(1280, baseline.Width);
            Assert.Equal(720, baseline.Height);
            Assert.Equal(frame.Rgb24.ToArray(), DecodeRgb(actual));
            var retro = state.ValueKind != JsonValueKind.Null && state.TryGetProperty("kind", out _);
            var regions = retro
                ? new[] { new Region("header", 0, 0, 1280, 160), new Region("columns", 0, 160, 1280, 320), new Region("actions", 0, 480, 1280, 240) }
                : new[] { new Region("header", 0, 0, 1280, 130), new Region("items", 0, 130, 930, 590), new Region("coverage", 930, 130, 350, 590) };
            var metrics = regions.Select(region => Measure(baseline, actual, region)).ToArray();
            foreach (var metric in metrics)
            {
                // The golden remains Chromium's image. Identity is separately reported, never claimed by this tolerance.
                if (metric.MeanAbsoluteChannelError > 2.5 || metric.ChangedPixelPercent > 6)
                {
                    failures.Add($"{name}/{metric.Name}: MAE={metric.MeanAbsoluteChannelError:F4}, changed={metric.ChangedPixelPercent:F3}%");
                }
            }

            var geometry = MeasureGeometry(state, baseline, actual);
            foreach (var metric in geometry.Where(metric => metric.MaximumChannelError > 1))
            {
                failures.Add($"{name}/{metric.Name}: geometry channel error {metric.MaximumChannelError} exceeds one-unit edge blend rounding.");
            }

            reports.Add(new
            {
                name,
                exactPixelIdentity = metrics.All(metric => metric.ChangedPixels == 0),
                nativePngSha256 = Hash(png),
                chromiumPngSha256 = Hash(File.ReadAllBytes(Path.Combine(assets, "chromium", $"{name}.png"))),
                nativeRgb24Sha256 = Hash(frame.Rgb24.Span),
                regions = metrics,
                geometry,
            });
        }

        File.WriteAllText(Path.Combine(output, "parity.json"), JsonSerializer.Serialize(new
        {
            clock = Clock,
            tolerance = new { maxMeanAbsoluteChannelError = 2.5, maxChangedPixelPercent = 6, maxGeometryChannelError = 1 },
            exactIdentityRequired = false,
            reason = "Identity fails: Chromium and native Skia differ in font rasterisation and GPOS text shaping/spacing. All pixels are measured. Geometry permits only one-channel-unit antialiased-edge blend rounding; no baseline is replaced.",
            fixtures = reports,
        }, ReportOptions));
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void Render_NativeCompletion_DoesNotPromiseStoredFollowUps()
    {
        using var renderer = new BoardRenderer();
        using var native = JsonDocument.Parse("""{"ended":true,"recording":false,"sprint":"Sprint 43"}""");
        using var legacy = JsonDocument.Parse("""{"ended":true,"sprint":"Sprint 43"}""");
        var nativePng = renderer.RenderPng(native.RootElement, Clock);
        Assert.NotEqual(Hash(renderer.RenderPng(legacy.RootElement, Clock)), Hash(nativePng));
        Assert.Equal(renderer.Render(new StandupBoardState { Ended = true, Recording = false, Sprint = "Sprint 43" }, Clock).ToPng(), nativePng);
        Assert.Equal(renderer.Render(null, Clock).Rgb24.ToArray(), renderer.RenderRgb24(default, Clock));
    }

    [Fact]
    public void ToRgb24_PaddedRows_PreservesRgbChannelAndTopDownRowOrder()
    {
        byte[] rgba = [1, 2, 3, 255, 4, 5, 6, 255, 99, 99, 99, 99, 7, 8, 9, 255, 10, 11, 12, 255, 99, 99, 99, 99];
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, BoardPixels.ToRgb24(rgba, 2, 2, 12));
        Assert.Throws<ArgumentException>(() => BoardPixels.ToRgb24(rgba.AsSpan(0, 23), 2, 2, 12));
    }

    [Fact]
    public void Render_RepeatedStateWithExplicitClock_IsDeterministicAndKeepsGeometry()
    {
        using var renderer = new BoardRenderer();
        var state = new StandupBoardState
        {
            Speaker = "Maya Lin",
            Focus = 4521,
            Items = [new BoardItem { Id = 4521, Covered = true }],
            Team = [new BoardMember { Name = "Maya Lin", Covered = 1, Total = 3 }, new BoardMember { Name = "Admin" }],
        };
        var first = renderer.Render(state, Clock);
        Assert.Equal(first.Rgb24.ToArray(), renderer.Render(state, Clock).Rgb24.ToArray());
        Assert.NotEqual(Hash(first.Rgb24.Span), Hash(renderer.Render(state, "1:23:46 PM").Rgb24.Span));
        using var image = SKBitmap.Decode(first.ToPng());
        Assert.Equal(SKColor.Parse("#0f172a"), image.GetPixel(0, 0));
        Assert.Equal(SKColor.Parse("#14532d"), image.GetPixel(60, 145));
        Assert.Equal(SKColor.Parse("#4ade80"), image.GetPixel(50, 145));
        Assert.Equal(SKColor.Parse("#38bdf8"), image.GetPixel(908, 150));
        Assert.Equal(SKColor.Parse("#4ade80"), image.GetPixel(1040, 214));
        Assert.Equal(SKColor.Parse("#1e293b"), image.GetPixel(1042, 214));
        Assert.Equal(SKColor.Parse("#1e293b"), image.GetPixel(949, 270));
        renderer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => renderer.Render(state, Clock));
        Assert.Equal(1280 * 720 * 3, first.Rgb24.Length);
    }

    [Fact]
    public void Render_OverflowItemsNotesActions_MatchesBrowserVisibleLimits()
    {
        using var renderer = new BoardRenderer();
        var item = new BoardItem { Id = 4521, Title = new string('x', 90) };
        var six = new StandupBoardState { Goal = new string('g', 90), Items = Enumerable.Repeat(item, 6).ToArray() };
        Assert.Equal(renderer.Render(six, Clock).Rgb24.ToArray(), renderer.Render(six with
        {
            Items = six.Items.Append(new BoardItem { Id = 9999 }).ToArray(),
            Goal = new string('g', 69) + "different clipped suffix",
        }, Clock).Rgb24.ToArray());

        var visibleNotes = Enumerable.Range(0, 5).Select(i => new RetroNote { Text = $"Note {i}" }).ToArray();
        var retro = new RetroBoardState
        {
            Columns = [new RetroColumn { Notes = new[] { new RetroNote { Text = "Hidden old note A" } }.Concat(visibleNotes).ToArray() }],
            Actions = Enumerable.Range(0, 3).Select(i => new RetroAction { Text = $"Action {i}", Owner = "Admin" }).ToArray(),
        };
        Assert.Equal(renderer.Render(retro, Clock).Rgb24.ToArray(), renderer.Render(retro with
        {
            Columns = [retro.Columns[0] with { Notes = new[] { new RetroNote { Text = "Hidden old note B" } }.Concat(visibleNotes).ToArray() }],
            Actions = retro.Actions.Append(new RetroAction { Text = "Invisible fourth action" }).ToArray(),
        }, Clock).Rgb24.ToArray());
    }

    private static string AssetsPath()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "src", "tests", "Zava.Scrum.MediaBot.Tests", "BoardGoldens");
            if (File.Exists(Path.Combine(path, "fixtures.json")))
            {
                return path;
            }
        }

        throw new DirectoryNotFoundException("Run board tests from a repository checkout containing BoardGoldens.");
    }

    private static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static Metrics[] MeasureGeometry(JsonElement state, SKBitmap expected, SKBitmap actual)
    {
        var regions = new List<Region>();
        if (state.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        if (state.TryGetProperty("kind", out _))
        {
            var i = 0;
            foreach (var column in state.GetProperty("columns").EnumerateArray())
            {
                _ = column;
                regions.Add(new Region($"column {i} accent", 48 + (i * 400), 160, 380, 6));
                regions.Add(new Region($"column {i} background", 48 + (i * 400), 460, 380, 20));
                i++;
            }
        }
        else
        {
            if (!state.GetProperty("ended").GetBoolean())
            {
                var count = Math.Min(6, state.GetProperty("items").GetArrayLength());
                for (var i = 0; i < count; i++)
                {
                    regions.Add(new Region($"item {i} background/stripe/focus", 46, 202 + (i * 84), 864, 8));
                }
            }

            var index = 0;
            foreach (var member in state.GetProperty("team").EnumerateArray())
            {
                _ = member;
                regions.Add(new Region($"coverage bar {index}", 948, 210 + (index * 56), 280, 10));
                index++;
            }
        }

        return regions.Select(region => Measure(expected, actual, region)).ToArray();
    }

    private static byte[] DecodeRgb(SKBitmap bitmap)
    {
        var bytes = new byte[bitmap.Width * bitmap.Height * 3];
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                var offset = ((y * bitmap.Width) + x) * 3;
                bytes[offset] = pixel.Red;
                bytes[offset + 1] = pixel.Green;
                bytes[offset + 2] = pixel.Blue;
            }
        }

        return bytes;
    }

    private static Metrics Measure(SKBitmap expected, SKBitmap actual, Region region)
    {
        long error = 0;
        var changed = 0;
        var maximum = 0;
        for (var y = region.Y; y < region.Y + region.Height; y++)
        {
            for (var x = region.X; x < region.X + region.Width; x++)
            {
                var e = expected.GetPixel(x, y);
                var a = actual.GetPixel(x, y);
                var red = Math.Abs(e.Red - a.Red);
                var green = Math.Abs(e.Green - a.Green);
                var blue = Math.Abs(e.Blue - a.Blue);
                var delta = red + green + blue;
                error += delta;
                maximum = Math.Max(maximum, Math.Max(red, Math.Max(green, blue)));
                changed += delta > 0 ? 1 : 0;
            }
        }

        var pixels = region.Width * region.Height;
        return new Metrics(region.Name, changed, 100d * changed / pixels, (double)error / (pixels * 3), maximum);
    }

    private sealed record Region(string Name, int X, int Y, int Width, int Height);
    private sealed record Metrics(string Name, int ChangedPixels, double ChangedPixelPercent, double MeanAbsoluteChannelError, int MaximumChannelError);
}
