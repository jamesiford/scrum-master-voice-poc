using System.Text.Json;
using Mcm.Scrum.Application.Abstractions.Ports;

namespace Mcm.Scrum.Infrastructure.Agents;

/// <summary>Reporting agent (SPEC 10.4): phrases narratives and answers questions over facts computed in code.</summary>
public sealed class FoundryReportingAgent(FoundryChat chat, IPromptStore prompts) : IReportingAgent
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public Task<string?> NarrateAsync(string kind, string factsJson, CancellationToken ct) =>
        CompleteAsync("reporting-narrative", $"{{\"kind\":\"{kind}\",\"facts\":{factsJson}}}", ct);

    /// <inheritdoc />
    public Task<string?> AnswerAsync(string question, string factsJson, CancellationToken ct) =>
        CompleteAsync("reporting-qa", $"{{\"question\":{JsonSerializer.Serialize(question)},\"facts\":{factsJson}}}", ct);

    private async Task<string?> CompleteAsync(string prompt, string user, CancellationToken ct)
    {
        if (!chat.IsConfigured)
        {
            return null;
        }

        var instructions = await prompts.GetAsync(prompt, ct);
        if (instructions is null)
        {
            return null;
        }

        var content = await chat.TryCompleteJsonAsync(instructions, user, ct);
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