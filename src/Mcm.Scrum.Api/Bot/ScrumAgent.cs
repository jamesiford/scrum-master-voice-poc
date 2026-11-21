using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Application.Features.FollowUps.ApproveFollowUp;
using Mcm.Scrum.Application.Features.FollowUps.RejectFollowUp;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.App.AdaptiveCards;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using Microsoft.Agents.Core.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Mcm.Scrum.Api.Bot;

/// <summary>
/// The Teams bot (SPEC 12): a thin presentation adapter. It remembers the conversation so cards can be sent proactively,
/// and turns the Approve and Reject card actions into commands. It holds no business rules.
/// </summary>
public sealed class ScrumAgent : AgentApplication
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConversationStore _conversations;

    /// <summary>Initializes a new instance of the <see cref="ScrumAgent"/> class.</summary>
    /// <param name="options">Agent application options.</param>
    /// <param name="scopes">Creates a scope per card action, because the mediator is scoped and the agent is resolved once at startup.</param>
    /// <param name="conversations">Conversation reference store.</param>
    public ScrumAgent(AgentApplicationOptions options, IServiceScopeFactory scopes, IConversationStore conversations)
        : base(options)
    {
        _scopes = scopes;
        _conversations = conversations;
        OnConversationUpdate("membersAdded", OnMembersAddedAsync);
        OnActivity(ActivityTypes.Message, OnMessageAsync);
        AdaptiveCards.OnActionExecute("approve", (ctx, _, data, ct) => DecideAsync(ctx, data, approve: true, ct));
        AdaptiveCards.OnActionExecute("reject", (ctx, _, data, ct) => DecideAsync(ctx, data, approve: false, ct));
    }

    private async Task OnMembersAddedAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken ct)
    {
        await RememberAsync(turnContext, ct);
        await turnContext.SendActivityAsync("Hi, I am the MCM Scrum Agent. After each standup I will post follow-ups here for you to approve or reject.", cancellationToken: ct);
    }

    private async Task OnMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken ct)
    {
        await RememberAsync(turnContext, ct);
        await turnContext.SendActivityAsync("Got it. Follow-up and recap cards from the daily standup will appear in this chat.", cancellationToken: ct);
    }

    private async Task<AdaptiveCardInvokeResponse> DecideAsync(ITurnContext turnContext, object data, bool approve, CancellationToken ct)
    {
        var raw = data is JsonElement e ? e.GetRawText() : JsonSerializer.Serialize(data);
        using var doc = JsonDocument.Parse(raw);
        if (!doc.RootElement.TryGetProperty("followUpId", out var idProperty) || !Guid.TryParse(idProperty.GetString(), out var id))
        {
            return AdaptiveCardInvokeResponseFactory.BadRequest("This card has no follow-up id.");
        }

        // The approver is the signed-in Teams user, so a person cannot approve as someone else (F7).
        var who = turnContext.Activity.From?.Name;
        if (string.IsNullOrWhiteSpace(who))
        {
            return AdaptiveCardInvokeResponseFactory.BadRequest("Teams did not tell me who you are.");
        }

        await using var scope = _scopes.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var result = approve
            ? await sender.Send(new ApproveFollowUpCommand(id, who), ct)
            : await sender.Send(new RejectFollowUpCommand(id, who), ct);
        return AdaptiveCardInvokeResponseFactory.Message(result.IsSuccess
            ? (approve ? "Approved. Writing to Azure DevOps." : "Rejected. Nothing was written.")
            : result.Error!.Message);
    }

    private async Task RememberAsync(ITurnContext turnContext, CancellationToken ct)
    {
        var reference = turnContext.Activity.GetConversationReference();
        await _conversations.SaveAsync(reference.Conversation.Id, ProtocolJsonSerializer.ToJson(reference), ct);
    }
}
