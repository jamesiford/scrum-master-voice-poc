namespace Mcm.Scrum.Application.Features.Standups;

/// <summary>Fills <c>{placeholder}</c> tokens in a prompt template.</summary>
public static class PromptTemplate
{
    /// <summary>Renders a template. Unknown placeholders are left as written so a typo is visible.</summary>
    /// <param name="template">Template text.</param>
    /// <param name="values">Placeholder values by name.</param>
    /// <returns>The rendered text.</returns>
    public static string Render(string template, IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(values);
        var result = template;
        foreach (var (key, value) in values)
        {
            result = result.Replace("{" + key + "}", value, StringComparison.Ordinal);
        }

        return result;
    }
}
