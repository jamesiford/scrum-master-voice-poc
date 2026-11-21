using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mcm.Scrum.Infrastructure.Persistence;

/// <summary>Reads and writes one JSON file atomically enough for a single-process PoC.</summary>
internal static class JsonFile
{
    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<T> ReadAsync<T>(string path, Func<T> empty, CancellationToken ct)
    {
        if (!File.Exists(path))
        {
            return empty();
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, ct) ?? empty();
    }

    public static async Task WriteAsync<T>(string path, T value, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, SerializerOptions), ct);
    }
}
