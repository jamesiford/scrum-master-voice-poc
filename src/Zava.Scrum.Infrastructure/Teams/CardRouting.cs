using Zava.Scrum.Application.Abstractions.Ports;

namespace Zava.Scrum.Infrastructure.Teams;

/// <summary>Chooses which stored Teams conversations a card is sent to (M9-R5). Pure, so it is unit tested without Teams.</summary>
public static class CardRouting
{
    // Teams names the chat of a meeting "19:meeting_...@thread.v2". The bot is only in that chat when it joined or was added to the meeting.
    private const string MeetingChatPrefix = "19:meeting_";

    /// <summary>Selects the conversations for a message.</summary>
    /// <param name="message">The message.</param>
    /// <param name="storedKeys">Keys of every stored conversation.</param>
    /// <returns>
    /// For <see cref="CardRoute.Recap"/>: the recap channel and any meeting chat the bot is in; every stored conversation when neither exists
    /// (DEMO fallback). For every other route: all stored conversations.
    /// PROD: route by Entra object id, with one conversation per member and the Scrum Master.
    /// </returns>
    public static IReadOnlyList<string> Select(TeamsMessage message, IReadOnlyList<string> storedKeys)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(storedKeys);
        if (message.Route != CardRoute.Recap)
        {
            return storedKeys;
        }

        var targets = storedKeys
            .Where(k => k == message.RecapChannelKey || k.StartsWith(MeetingChatPrefix, StringComparison.Ordinal))
            .ToList();
        return targets.Count > 0 ? targets : storedKeys;
    }
}
