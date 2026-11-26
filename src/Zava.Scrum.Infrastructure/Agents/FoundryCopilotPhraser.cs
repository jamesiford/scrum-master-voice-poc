using System.Text.Json;
using Zava.Scrum.Application.Abstractions.Ports;

namespace Zava.Scrum.Infrastructure.Agents;

/// <summary>Phrases only an introduction, keeping readable work item content, figures and links out of model output.</summary>
public sealed class FoundryCopilotPhraser(FoundryChat chat, IPromptStore prompts) : ICopilotPhraser
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<string?> PhraseAsync(string summary, CancellationToken ct)
    {
        if (!chat.IsConfigured || await prompts.GetAsync("copilot-qa", ct) is not { } instructions)
        {
            return null;
        }

        var content = await chat.TryCompleteJsonAsync(instructions, JsonSerializer.Serialize(new { summary }), ct);
        if (content is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Reply>(content, Json)?.Text;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record Reply(string? Text);
}
