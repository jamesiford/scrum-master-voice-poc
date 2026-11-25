namespace Zava.Scrum.Domain.Teams;

/// <summary>What a bound Teams conversation is used for.</summary>
public enum BindingPurpose
{
    /// <summary>Recap and follow-up cards.</summary>
    Recap,

    /// <summary>The meeting the standup runs in.</summary>
    Standup,

    /// <summary>Sprint planning cards and the planning chat command.</summary>
    Planning,
}

/// <summary>
/// Binds one Teams channel or chat to a team, so a conversation resolves to its team without any configuration
/// (ADR 0018). Exactly one of <paramref name="ChannelId"/> and <paramref name="ChatId"/> names the conversation.
/// </summary>
/// <param name="TenantId">The Entra tenant of the Teams team. Must be the deployment's tenant.</param>
/// <param name="TeamId">The Teams team (group) id, when the binding is a channel.</param>
/// <param name="ChannelId">The channel conversation id, for a channel binding.</param>
/// <param name="ChatId">The chat conversation id, for a chat binding.</param>
/// <param name="Purpose">What the conversation is for.</param>
public sealed record TeamsBinding(string TenantId, string? TeamId, string? ChannelId, string? ChatId, BindingPurpose Purpose)
{
    /// <summary>Gets the conversation id this binding names.</summary>
    public string? ConversationId => ChannelId ?? ChatId;

    /// <summary>Checks whether a conversation is the one this binding names.</summary>
    /// <param name="conversationId">The Teams conversation id from the activity.</param>
    /// <returns><see langword="true"/> when it matches (ids are compared without the message suffix Teams appends to channel threads).</returns>
    public bool Matches(string? conversationId) =>
        !string.IsNullOrWhiteSpace(conversationId) && ConversationId is { } mine
        && string.Equals(Strip(mine), Strip(conversationId), StringComparison.OrdinalIgnoreCase);

    private static string Strip(string id) => id.Split(";messageid=", 2)[0];
}

/// <summary>Which ceremonies a team runs. A ceremony that is off is neither scheduled nor offered.</summary>
/// <param name="DailyScrum">The daily standup.</param>
/// <param name="Planning">Sprint planning.</param>
/// <param name="Retrospective">The retrospective.</param>
/// <param name="Review">The sprint review.</param>
public sealed record CeremoniesEnabled(bool DailyScrum = true, bool Planning = true, bool Retrospective = true, bool Review = false);
