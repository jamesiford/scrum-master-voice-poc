using System.Text.Json;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Standups;
using Microsoft.Extensions.Logging;

namespace Mcm.Scrum.Infrastructure.Agents;

/// <summary>
/// Backlog and Risk agent: phrases questions and a summary for a pre-brief built in code (SPEC 10.2).
/// The caller re-checks every id (rules 2 and 3).
/// </summary>
/// <remarks>PROD: move behind a Microsoft Agent Framework agent when it needs tools or memory (ADR 0002).</remarks>
public sealed class FoundryBacklogRiskAgent(FoundryChat chat, IPromptStore prompts, ILogger<FoundryBacklogRiskAgent> logger) : IBacklogRiskAgent
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<BacklogRiskSuggestion?> SuggestAsync(PreBrief brief, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(brief);
        if (!chat.IsConfigured)
        {
            return null;
        }

        var instructions = await prompts.GetAsync("backlog-risk", ct);
        if (instructions is null)
        {
            logger.LogWarning("Prompt 'backlog-risk' not found; using deterministic questions");
            return null;
        }

        var facts = new
        {
            brief.SprintGoal,
            brief.DaysLeft,
            brief.Burndown,
            brief.TeamRisks,
            Members = brief.Members.Select(m => new { m.MemberId, Name = m.DisplayName, m.Items }),
            brief.UnassignedItems,
        };
        var content = await chat.TryCompleteJsonAsync(instructions, JsonSerializer.Serialize(facts, Json), ct);
        return content is null ? null : Parse(content);
    }

    private static BacklogRiskSuggestion? Parse(string content)
    {
        try
        {
            var reply = JsonSerializer.Deserialize<ModelReply>(content, Json);
            if (reply?.Questions is null)
            {
                return null;
            }

            var questions = reply.Questions
                .Where(q => !string.IsNullOrWhiteSpace(q.MemberId) && !string.IsNullOrWhiteSpace(q.Question))
                .GroupBy(q => q.MemberId!)
                .ToDictionary(g => g.Key, g => g.First().Question!);
            return new BacklogRiskSuggestion(reply.Summary ?? string.Empty, questions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record ModelReply(string? Summary, List<ModelQuestion>? Questions);

    private sealed record ModelQuestion(string? MemberId, string? Question);
}
