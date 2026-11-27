using Microsoft.Agents.Core.Models;
using Zava.Scrum.Application.Features.Reports.AnswerCopilotQuestion;

namespace Zava.Scrum.Api.Bot;

/// <summary>Renders code-generated source links in a read-only card, without exposing a broader agent-identity board.</summary>
public static class CopilotAnswerCard
{
    /// <summary>Creates a compact answer card with permission-protected Azure DevOps links.</summary>
    /// <param name="answer">The authorised answer.</param>
    /// <returns>The attachment for the same final streamed message.</returns>
    public static Attachment Create(CopilotAnswerDto answer) => new()
    {
        ContentType = "application/vnd.microsoft.card.adaptive",
        Content = new
        {
            type = "AdaptiveCard",
            version = "1.5",
            body = new object[]
            {
                new { type = "TextBlock", text = $"Sprint facts ({answer.TeamId})", weight = "Bolder", wrap = true },
                new { type = "TextBlock", text = answer.Text, wrap = true },
            },
            actions = answer.ItemLinks.Take(5).Select(url => new
            {
                type = "Action.OpenUrl",
                title = $"Open #{url.Split('/')[^1]} in Azure DevOps",
                url,
            }).ToArray(),
        },
    };
}
