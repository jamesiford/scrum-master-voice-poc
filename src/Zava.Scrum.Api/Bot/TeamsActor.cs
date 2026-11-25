using System.Text.Json;
using Microsoft.Agents.Core.Models;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Identity;

namespace Zava.Scrum.Api.Bot;

/// <summary>
/// Turns a Teams activity into an <see cref="ActorIdentity"/> from <c>Activity.From.AadObjectId</c> and the activity's tenant. The Bot Service
/// token on <c>/api/messages</c> is validated before the activity reaches the bot, so these fields come from Teams, not from the sender.
/// The display name is carried for text only.
/// </summary>
public static class TeamsActor
{
    /// <summary>Builds the identity from an activity.</summary>
    /// <param name="activity">The inbound activity.</param>
    /// <returns>The identity, or a validation error when the activity carries no Entra object id or tenant.</returns>
    public static Result<ActorIdentity> From(IActivity? activity)
    {
        if (activity is null)
        {
            return Result<ActorIdentity>.Failure(IdentityErrors.ObjectIdRequired);
        }

        var tenant = activity.Conversation?.TenantId ?? TenantFromChannelData(activity.ChannelData);
        return ActorIdentity.Create(activity.From?.AadObjectId, tenant, activity.From?.Name, null, ActorSource.Teams, "bot-service-token");
    }

    private static string? TenantFromChannelData(object? channelData)
    {
        if (channelData is null)
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(channelData is JsonElement e ? e.GetRawText() : JsonSerializer.Serialize(channelData));
            return doc.RootElement.TryGetProperty("tenant", out var tenant) && tenant.TryGetProperty("id", out var id) ? id.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
