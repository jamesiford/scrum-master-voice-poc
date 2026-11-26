using System.Text.RegularExpressions;
using Microsoft.Agents.Core.Models;
using Zava.Scrum.Application.Features.Reports.AnswerCopilotQuestion;

namespace Zava.Scrum.Api.Bot;

/// <summary>Recognises Copilot parent/subchannel activities and strips the bot mention before skill routing.</summary>
public static partial class CopilotMessage
{
    /// <summary>Checks the channel and subchannel exposed by the Agents SDK.</summary>
    /// <param name="activity">The authenticated activity.</param>
    /// <returns>Whether it came from Microsoft 365 Copilot.</returns>
    public static bool IsCopilot(IActivity activity) =>
        activity.ChannelId == Channels.M365Copilot ||
        string.Equals(activity.ChannelId?.SubChannel, Channels.M365CopilotSubChannel, StringComparison.OrdinalIgnoreCase);

    /// <summary>Reads user text without the recipient mention.</summary>
    /// <param name="activity">The incoming message.</param>
    /// <returns>Plain question text.</returns>
    public static string Question(IActivity activity) => Mention().Replace(activity.Text ?? string.Empty, string.Empty).Trim();

    /// <summary>Checks whether this is a private Copilot turn or a supported skill in another chat channel.</summary>
    /// <param name="activity">The incoming activity.</param>
    /// <returns>Whether the permission-scoped route must own the turn.</returns>
    public static bool IsSkillTurn(IActivity activity) =>
        activity.Type == ActivityTypes.Message &&
        (IsCopilot(activity) || CopilotQuestions.TryParse(Question(activity), out _, out _));

    [GeneratedRegex(@"<at>.*?</at>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Mention();
}
