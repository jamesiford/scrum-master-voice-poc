namespace Zava.Scrum.Domain.Voice;

/// <summary>Resolves only unambiguous relative transcript phrases from code-owned calendar facts. Does not create work-item changes.</summary>
public static class SpokenDateResolver
{
    /// <summary>Resolves a selected verbatim expression without asking a model for an absolute date.</summary>
    /// <param name="phrase">The candidate selected from the transcript.</param>
    /// <param name="context">Team-local date and actual iteration dates.</param>
    /// <returns>A date or an explicit reason for leaving the note unresolved.</returns>
    public static SpokenDateResolution Resolve(string? phrase, SpokenDateContext? context)
    {
        if (string.IsNullOrWhiteSpace(phrase)) { return new(null, "no_revised_date"); }
        if (context is null) { return new(null, "calendar_unavailable"); }
        var expression = phrase.Trim().ToLowerInvariant();
        foreach (var prefix in new[] { "by ", "on ", "until " })
        {
            if (expression.StartsWith(prefix, StringComparison.Ordinal)) { expression = expression[prefix.Length..]; break; }
        }

        if (expression is "today" or "tonight") { return new(context.Today, "resolved"); }
        if (expression == "tomorrow") { return new(context.Today.AddDays(1), "resolved"); }
        if (expression is "end of sprint" or "end of the sprint" or "end of this sprint")
        {
            var current = context.CurrentIteration;
            return current is not null && current.Start <= context.Today && current.End >= current.Start
                ? Future(current.End, context.Today, "current_iteration_unavailable")
                : new(null, "current_iteration_unavailable");
        }
        if (expression is "end of next sprint" or "end of the next sprint")
        {
            var next = context.NextIteration;
            return next is not null && context.CurrentIteration is { } current
                && current.Start <= context.Today && current.End >= current.Start
                && next.Start > current.End && next.Start > context.Today && next.End >= next.Start
                ? Future(next.End, context.Today, "next_iteration_unavailable")
                : new(null, "next_iteration_unavailable");
        }

        var nextWeekday = expression.StartsWith("next ", StringComparison.Ordinal);
        var thisWeekday = expression.StartsWith("this ", StringComparison.Ordinal);
        var weekday = nextWeekday || thisWeekday ? expression[5..] : expression;
        if (Enum.TryParse<DayOfWeek>(weekday, true, out var day) && Enum.IsDefined(day))
        {
            var days = ((int)day - (int)context.Today.DayOfWeek + 7) % 7;
            if (nextWeekday && days == 0) { days = 7; }
            if (thisWeekday && (int)day < (int)context.Today.DayOfWeek) { return new(null, "past_date"); }
            return new(context.Today.AddDays(days), "resolved");
        }

        return new(null, phrase.Contains('/', StringComparison.Ordinal) ? "ambiguous_numeric_date" : "ambiguous_period_or_date");
    }

    private static SpokenDateResolution Future(DateOnly? date, DateOnly today, string unavailable) =>
        date is null ? new(null, unavailable) : date < today ? new(null, "past_date") : new(date, "resolved");
}
