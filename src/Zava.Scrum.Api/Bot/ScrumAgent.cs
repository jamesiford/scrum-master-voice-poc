using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.FollowUps.ApproveFollowUp;
using Zava.Scrum.Application.Features.FollowUps.RejectFollowUp;
using Zava.Scrum.Application.Features.Calendars.ApproveSprintCalendar;
using Zava.Scrum.Application.Features.Calendars.RejectSprintCalendar;
using Zava.Scrum.Application.Features.Planning.ApprovePlan;
using Zava.Scrum.Application.Features.Planning.PreparePlanning;
using Zava.Scrum.Application.Features.Planning.RejectPlan;
using Zava.Scrum.Application.Features.Reports.AnswerTeamQuestion;
using Zava.Scrum.Application.Features.Teams.SetRecapChannel;
using Zava.Scrum.Domain.FollowUps;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.App.AdaptiveCards;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using Microsoft.Agents.Core.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Zava.Scrum.Api.Bot;

/// <summary>
/// The Teams bot (SPEC 12): a thin presentation adapter. It remembers the conversation so cards can be sent proactively,
/// and turns the Approve and Reject card actions into commands. It holds no business rules.
/// </summary>
public sealed partial class ScrumAgent : AgentApplication
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConversationStore _conversations;
    private readonly ScrumMasterRoster _scrumMasters;
    private readonly string _teamId;

    /// <summary>Initializes a new instance of the <see cref="ScrumAgent"/> class.</summary>
    /// <param name="options">Agent application options.</param>
    /// <param name="scopes">Creates a scope per card action, because the mediator is scoped and the agent is resolved once at startup.</param>
    /// <param name="conversations">Conversation reference store.</param>
    /// <param name="configuration">Configuration, for the team this bot serves.</param>
    /// <param name="scrumMasters">Who may run Scrum Master chat commands.</param>
    public ScrumAgent(AgentApplicationOptions options, IServiceScopeFactory scopes, IConversationStore conversations, IConfiguration configuration, ScrumMasterRoster scrumMasters)
        : base(options)
    {
        _scopes = scopes;
        _conversations = conversations;
        _scrumMasters = scrumMasters;
        _teamId = configuration["Team:Id"] ?? "team-payments";
        OnConversationUpdate("membersAdded", OnMembersAddedAsync);
        OnActivity(ActivityTypes.Message, OnMessageAsync);
        AdaptiveCards.OnActionExecute("approve", (ctx, _, data, ct) => DecideAsync(ctx, data, approve: true, ct));
        AdaptiveCards.OnActionExecute("reject", (ctx, _, data, ct) => DecideAsync(ctx, data, approve: false, ct));
        AdaptiveCards.OnActionExecute("approve-plan", (ctx, _, data, ct) => DecidePlanAsync(ctx, data, approve: true, ct));
        AdaptiveCards.OnActionExecute("reject-plan", (ctx, _, data, ct) => DecidePlanAsync(ctx, data, approve: false, ct));
        AdaptiveCards.OnActionExecute("approve-calendar", (ctx, _, data, ct) => DecideCalendarAsync(ctx, data, approve: true, ct));
        AdaptiveCards.OnActionExecute("reject-calendar", (ctx, _, data, ct) => DecideCalendarAsync(ctx, data, approve: false, ct));
    }

    private async Task OnMembersAddedAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken ct)
    {
        await RememberAsync(turnContext, ct);
        await CaptureRecapChannelAsync(turnContext, ct);
        await turnContext.SendActivityAsync("Hi, I am the MCM Scrum Agent. After each standup I will post follow-ups here for you to approve or reject.", cancellationToken: ct);
    }

    // Installed in a team channel: that channel becomes the recap channel (M9-R5). Personal and group chats do not.
    private async Task CaptureRecapChannelAsync(ITurnContext turnContext, CancellationToken ct)
    {
        var conversation = turnContext.Activity.Conversation;
        if (!string.Equals(conversation?.ConversationType, "channel", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(conversation?.Id))
        {
            return;
        }

        await using var scope = _scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(new SetRecapChannelCommand(_teamId, conversation.Id), ct);
    }

    private async Task OnMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken ct)
    {
        await RememberAsync(turnContext, ct);
        var question = turnContext.Activity.Text?.Trim();
        if (string.IsNullOrWhiteSpace(question))
        {
            return;
        }

        if (PreparePlanningPhrase().IsMatch(question))
        {
            await PreparePlanningAsync(turnContext, ct);
            return;
        }

        await using var scope = _scopes.CreateAsyncScope();
        var answer = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new AnswerTeamQuestionQuery(_teamId, question), ct);
        await turnContext.SendActivityAsync(answer.IsSuccess ? answer.Value : answer.Error!.Message, cancellationToken: ct);
    }

    // "prepare sprint planning" is a Scrum Master command: it prepares the pack and the card arrives like any other.
    private async Task PreparePlanningAsync(ITurnContext turnContext, CancellationToken ct)
    {
        if (!_scrumMasters.IsScrumMaster(turnContext.Activity.From?.Name))
        {
            await turnContext.SendActivityAsync("Only the Scrum Master can prepare sprint planning. Ask them, or add your name to Team:ScrumMasters.", cancellationToken: ct);
            return;
        }

        await using var scope = _scopes.CreateAsyncScope();
        var prepared = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new PreparePlanningCommand(_teamId), ct);
        await turnContext.SendActivityAsync(
            prepared.IsSuccess ? "I prepared the sprint planning pack. The card with Approve plan and Reject is in this chat and in the portal." : prepared.Error!.Message,
            cancellationToken: ct);
    }

    // The approver is the signed-in Teams user (Activity.From.Name), the same rule as follow-ups (F7).
    private async Task<AdaptiveCardInvokeResponse> DecidePlanAsync(ITurnContext turnContext, object data, bool approve, CancellationToken ct)
    {
        var raw = data is JsonElement e ? e.GetRawText() : JsonSerializer.Serialize(data);
        using var doc = JsonDocument.Parse(raw);
        if (!doc.RootElement.TryGetProperty("packId", out var idProperty) || !Guid.TryParse(idProperty.GetString(), out var id))
        {
            return AdaptiveCardInvokeResponseFactory.BadRequest("This card has no planning pack id.");
        }

        var who = turnContext.Activity.From?.Name;
        if (string.IsNullOrWhiteSpace(who))
        {
            return AdaptiveCardInvokeResponseFactory.BadRequest("Teams did not tell me who you are.");
        }

        await using var scope = _scopes.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        string? failure;
        if (approve)
        {
            var approved = await sender.Send(new ApprovePlanCommand(id, who, [], ApprovalChannel.Teams), ct);
            failure = approved.IsSuccess ? null : approved.Error!.Message;
        }
        else
        {
            var rejected = await sender.Send(new RejectPlanCommand(id, who), ct);
            failure = rejected.IsSuccess ? null : rejected.Error!.Message;
        }

        return AdaptiveCardInvokeResponseFactory.Message(failure ?? (approve ? "Plan approved. Writing to Azure DevOps." : "Plan rejected. Nothing was written."));
    }

    // The approver is the signed-in Teams user (Activity.From.Name), the same rule as follow-ups (F7).
    private async Task<AdaptiveCardInvokeResponse> DecideCalendarAsync(ITurnContext turnContext, object data, bool approve, CancellationToken ct)
    {
        var raw = data is JsonElement e ? e.GetRawText() : JsonSerializer.Serialize(data);
        using var doc = JsonDocument.Parse(raw);
        if (!doc.RootElement.TryGetProperty("calendarId", out var idProperty) || !Guid.TryParse(idProperty.GetString(), out var id))
        {
            return AdaptiveCardInvokeResponseFactory.BadRequest("This card has no calendar id.");
        }

        var who = turnContext.Activity.From?.Name;
        if (string.IsNullOrWhiteSpace(who))
        {
            return AdaptiveCardInvokeResponseFactory.BadRequest("Teams did not tell me who you are.");
        }

        await using var scope = _scopes.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var failure = approve
            ? (await sender.Send(new ApproveSprintCalendarCommand(id, who), ct)).Error?.Message
            : (await sender.Send(new RejectSprintCalendarCommand(id, who), ct)).Error?.Message;
        return AdaptiveCardInvokeResponseFactory.Message(failure ?? (approve ? "Calendar approved. The meetings are in the calendar." : "Calendar rejected. Nothing was created."));
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

    [GeneratedRegex(@"\bprepare\s+(?:the\s+)?(?:sprint\s+)?planning\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PreparePlanningPhrase();

    private async Task RememberAsync(ITurnContext turnContext, CancellationToken ct)
    {
        var reference = turnContext.Activity.GetConversationReference();
        await _conversations.SaveAsync(reference.Conversation.Id, ProtocolJsonSerializer.ToJson(reference), ct);
    }
}
