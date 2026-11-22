using System.Text.Json;
using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.Stories.DraftUserStory;

/// <summary>Asks the Story Writer prompt for a draft and falls back to a plain template when no model is available.</summary>
internal sealed class DraftUserStoryHandler(IDraftingAgent agent) : IRequestHandler<DraftUserStoryQuery, Result<StoryDraft>>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<Result<StoryDraft>> Handle(DraftUserStoryQuery request, CancellationToken ct)
    {
        var topic = request.Topic.Trim();
        var reply = await agent.DraftJsonAsync("story-writer", JsonSerializer.Serialize(new { topic }, Json), ct);
        var parsed = Parse(reply);
        return Result<StoryDraft>.Success(parsed ?? Template(topic));
    }

    private static StoryDraft? Parse(string? reply)
    {
        if (reply is null)
        {
            return null;
        }

        try
        {
            var draft = JsonSerializer.Deserialize<Reply>(reply, Json);
            return draft is { Title: { Length: > 0 } title, Story: { Length: > 0 } story, AcceptanceCriteria.Count: > 0 }
                ? new StoryDraft(title, story, draft.AcceptanceCriteria, true)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static StoryDraft Template(string topic) => new(
        topic.Length > 80 ? topic[..80] : topic,
        $"As a team member, I want {topic}, so that the sprint goal is not put at risk.",
        ["The behaviour is described and agreed with the product owner.", "The change is covered by an automated test.", "The team has demonstrated it in the sprint review."],
        false);

    private sealed record Reply(string? Title, string? Story, List<string>? AcceptanceCriteria);
}
