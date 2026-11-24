namespace Zava.Scrum.Infrastructure.Agents;

/// <summary>Maps the snake_case words used in model prompts and decision-model options ("ask_item_info") to PascalCase enum members.</summary>
internal static class SnakeEnum
{
    /// <summary>Parses a word to a defined enum value.</summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The word, in any case, with or without underscores.</param>
    /// <param name="result">The value when parsed.</param>
    /// <returns><see langword="true"/> when the word names a defined member.</returns>
    public static bool TryParse<T>(string? value, out T result)
        where T : struct, Enum
    {
        result = default;
        return !string.IsNullOrWhiteSpace(value)
            && Enum.TryParse(value.Replace("_", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal), ignoreCase: true, out result)
            && Enum.IsDefined(result);
    }
}
