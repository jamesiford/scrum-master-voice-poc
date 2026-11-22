namespace Zava.Scrum.Domain.Standups;

/// <summary>Column titles and the spoken prompt for each <see cref="RetroFormat"/>. One source for the board, the cards and the agent.</summary>
public static class RetroFormats
{
    /// <summary>Gets the title of a column.</summary>
    /// <param name="format">The format.</param>
    /// <param name="category">The category.</param>
    /// <returns>For example "Went well" or "Start".</returns>
    public static string Label(RetroFormat format, RetroCategory category) => (format, category) switch
    {
        (RetroFormat.WentWell, RetroCategory.Continue) => "Went well",
        (RetroFormat.WentWell, RetroCategory.Stop) => "Do better",
        (RetroFormat.WentWell, RetroCategory.Start) => "Missing",
        (_, RetroCategory.Start) => "Start",
        (_, RetroCategory.Stop) => "Stop",
        _ => "Continue",
    };

    /// <summary>Gets what the agent asks each member.</summary>
    /// <param name="format">The format.</param>
    /// <returns>A phrase such as "what went well, what we should do better and what is missing".</returns>
    public static string Prompt(RetroFormat format) => format == RetroFormat.WentWell
        ? "what went well, what we should do better, and what is missing"
        : "what we should start doing, what we should stop doing, and what we should keep doing";
}
