using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Identity;
using Zava.Scrum.Application.Features.Teams;
using Zava.Scrum.Application.Features.Teams.BindConversation;
using Zava.Scrum.Domain.Teams;
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
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Zava.Scrum.Api.Bot;

/// <summary>
/// The Teams bot (SPEC 12): a thin presentation adapter. It remembers the conversation so cards can be sent proactively,
/// and turns the Approve and Reject card actions into commands. It holds no business rules.
/// </summary>
public sealed partial class ScrumAgent : AgentApplication
{
    private readonly string[]? _ssoHandlers;
    private readonly IServiceScopeFactory _scopes;
    private readonly IConversationStore _conversations;
    private readonly IIdentityPolicy _identity;

    /// <summary>Initializes a new instance of the <see cref="ScrumAgent"/> class.</summary>
    /// <param name="options">Agent application options.</param>
    /// <param name="scopes">Creates a scope per card action, because the mediator is scoped and the agent is resolved once at startup.</param>
    /// <param name="conversations">Conversation reference store.</param>
    /// <param name="identity">Who counts as an administrator (by Entra object id) for Scrum Master chat commands.</param>
    /// <param name="configuration">Tells the agent whether the single sign-on OAuth handler is configured.</param>
    public ScrumAgent(AgentApplicationOptions options, IServiceScopeFactory scopes, IConversationStore conversations, IIdentityPolicy identity, IConfiguration configuration)
        : base(options)
    {
        // Approving needs the approver's own sign-in (ADR 0020). Without the OAuth connection there is no sign-in, so approvals are refused, not faked.
        _ssoHandlers = string.IsNullOrWhiteSpace(configuration[$"AgentApplication:UserAuthorization:Handlers:{SsoHandler}:Settings:AzureBotOAuthConnectionName"]) ? null : [SsoHandler];
        _scopes = scopes;
        _conversations = conversations;
        _identity = identity;
        RegisterMeetingEvents();
        OnConversationUpdate("membersAdded", OnMembersAddedAsync);
        RegisterCopilotSkills();
        OnActivity(ActivityTypes.Message, OnMessageAsync);
        AdaptiveCards.OnActionExecute("bind-team", (ctx, _, data, ct) => BindAsync(ctx, data, ct));
        AdaptiveCards.OnActionExecute("approve", (ctx, state, data, ct) => DecideAsync(ctx, state, data, approve: true, ct), autoSignInHandlers: _ssoHandlers!);
        AdaptiveCards.OnActionExecute("reject", (ctx, state, data, ct) => DecideAsync(ctx, state, data, approve: false, ct));
        AdaptiveCards.OnActionExecute("approve-plan", (ctx, state, data, ct) => DecidePlanAsync(ctx, state, data, approve: true, ct), autoSignInHandlers: _ssoHandlers!);
        AdaptiveCards.OnActionExecute("reject-plan", (ctx, state, data, ct) => DecidePlanAsync(ctx, state, data, approve: false, ct));
        AdaptiveCards.OnActionExecute("approve-calendar", (ctx, _, data, ct) => DecideCalendarAsync(ctx, data, approve: true, ct));
        AdaptiveCards.OnActionExecute("reject-calendar", (ctx, _, data, ct) => DecideCalendarAsync(ctx, data, approve: false, ct));
    }

    private async Task OnMembersAddedAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken ct)
    {
        if (CopilotMessage.IsCopilot(turnContext.Activity))
        {
            await SendCopilotWelcomeAsync(turnContext, ct);
            return;
        }

        await RememberAsync(turnContext, ct);
        var team = await ResolveTeamAsync(turnContext, ct);
        if (team is null)
        {
            return;
        }

        await turnContext.SendActivityAsync($"Hi, I am the Zava Scrum Agent for {team.Name}. After each standup I will post follow-ups here for you to approve or reject.", cancellationToken: ct);
    }

    private async Task OnMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken ct)
    {
        await RememberAsync(turnContext, ct);
        var question = turnContext.Activity.Text?.Trim();
        if (string.IsNullOrWhiteSpace(question))
        {
            return;
        }

        // The conversation decides the team (its bindings). An unbound conversation gets the onboarding card and nothing else happens (ADR 0018).
        var team = await ResolveTeamAsync(turnContext, ct);
        if (team is null)
        {
            return;
        }

        if (PreparePlanningPhrase().IsMatch(question))
        {
            await PreparePlanningAsync(turnContext, team, ct);
            return;
        }

        await using var scope = _scopes.CreateAsyncScope();
        var answer = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new AnswerTeamQuestionQuery(team.Id, question), ct);
        await turnContext.SendActivityAsync(answer.IsSuccess ? answer.Value : answer.Error!.Message, cancellationToken: ct);
    }

    // "prepare sprint planning" is a Scrum Master command: it prepares the pack and the card arrives like any other.
    private async Task PreparePlanningAsync(ITurnContext turnContext, Team team, CancellationToken ct)
    {
        // Decided by Entra object id and team role, never by display name (ADR 0017, 0018): an Admin or this team's lead.
        await using var scope = _scopes.CreateAsyncScope();
        var allowed = await CanOperateAsync(scope, turnContext, team, ct);
        if (!allowed)
        {
            await turnContext.SendActivityAsync("Only an administrator or this team's lead can prepare sprint planning.", cancellationToken: ct);
            return;
        }

        var prepared = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new PreparePlanningCommand(team.Id), ct);
        await turnContext.SendActivityAsync(
            prepared.IsSuccess ? "I prepared the sprint planning pack. The card with Approve plan and Reject is in this chat and in the portal." : prepared.Error!.Message,
            cancellationToken: ct);
    }

    // The approver is the Entra identity on the activity (From.AadObjectId and tenant), the same rule as follow-ups (F7, ADR 0017).
    private async Task<AdaptiveCardInvokeResponse> DecidePlanAsync(ITurnContext turnContext, ITurnState turnState, object data, bool approve, CancellationToken ct)
    {
        var raw = data is JsonElement e ? e.GetRawText() : JsonSerializer.Serialize(data);
        using var doc = JsonDocument.Parse(raw);
        if (!doc.RootElement.TryGetProperty("packId", out var idProperty) || !Guid.TryParse(idProperty.GetString(), out var id))
        {
            return AdaptiveCardInvokeResponseFactory.BadRequest("This card has no planning pack id.");
        }

        var who = TeamsActor.From(turnContext.Activity);
        if (!who.IsSuccess)
        {
            return AdaptiveCardInvokeResponseFactory.BadRequest("Teams did not tell me who you are (no Entra identity on the activity).");
        }

        var decision = DecisionContext.FromTeams(who.Value);
        await using var scope = _scopes.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        string? failure;
        if (approve)
        {
            if (!await SignInApproverAsync(scope, turnContext, turnState, ct))
            {
                return AdaptiveCardInvokeResponseFactory.Message(SignInNeeded);
            }

            var approved = await sender.Send(new ApprovePlanCommand(id, decision, []), ct);
            failure = approved.IsSuccess ? null : approved.Error!.Message;
        }
        else
        {
            var rejected = await sender.Send(new RejectPlanCommand(id, decision), ct);
            failure = rejected.IsSuccess ? null : rejected.Error!.Message;
        }

        return AdaptiveCardInvokeResponseFactory.Message(failure ?? (approve ? "Plan approved. Writing to Azure DevOps." : "Plan rejected. Nothing was written."));
    }

    // The approver is the Entra identity on the activity (From.AadObjectId and tenant), the same rule as follow-ups (F7, ADR 0017).
    private async Task<AdaptiveCardInvokeResponse> DecideCalendarAsync(ITurnContext turnContext, object data, bool approve, CancellationToken ct)
    {
        var raw = data is JsonElement e ? e.GetRawText() : JsonSerializer.Serialize(data);
        using var doc = JsonDocument.Parse(raw);
        if (!doc.RootElement.TryGetProperty("calendarId", out var idProperty) || !Guid.TryParse(idProperty.GetString(), out var id))
        {
            return AdaptiveCardInvokeResponseFactory.BadRequest("This card has no calendar id.");
        }

        var who = TeamsActor.From(turnContext.Activity);
        if (!who.IsSuccess)
        {
            return AdaptiveCardInvokeResponseFactory.BadRequest("Teams did not tell me who you are (no Entra identity on the activity).");
        }

        var decision = DecisionContext.FromTeams(who.Value);
        await using var scope = _scopes.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var failure = approve
            ? (await sender.Send(new ApproveSprintCalendarCommand(id, decision), ct)).Error?.Message
            : (await sender.Send(new RejectSprintCalendarCommand(id, decision), ct)).Error?.Message;
        return AdaptiveCardInvokeResponseFactory.Message(failure ?? (approve ? "Calendar approved. The meetings are in the calendar." : "Calendar rejected. Nothing was created."));
    }

    private async Task<AdaptiveCardInvokeResponse> DecideAsync(ITurnContext turnContext, ITurnState turnState, object data, bool approve, CancellationToken ct)
    {
        var raw = data is JsonElement e ? e.GetRawText() : JsonSerializer.Serialize(data);
        using var doc = JsonDocument.Parse(raw);
        if (!doc.RootElement.TryGetProperty("followUpId", out var idProperty) || !Guid.TryParse(idProperty.GetString(), out var id))
        {
            return AdaptiveCardInvokeResponseFactory.BadRequest("This card has no follow-up id.");
        }

        // The approver is the Entra identity Teams put on the activity, so a person cannot approve as someone else (F7, ADR 0017).
        var who = TeamsActor.From(turnContext.Activity);
        if (!who.IsSuccess)
        {
            return AdaptiveCardInvokeResponseFactory.BadRequest("Teams did not tell me who you are (no Entra identity on the activity).");
        }

        var decision = DecisionContext.FromTeams(who.Value);
        await using var scope = _scopes.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        if (approve && !await SignInApproverAsync(scope, turnContext, turnState, ct))
        {
            return AdaptiveCardInvokeResponseFactory.Message(SignInNeeded);
        }

        var result = approve
            ? await sender.Send(new ApproveFollowUpCommand(id, decision), ct)
            : await sender.Send(new RejectFollowUpCommand(id, decision), ct);
        return AdaptiveCardInvokeResponseFactory.Message(result.IsSuccess
            ? (approve ? "Approved. Writing to Azure DevOps." : "Rejected. Nothing was written.")
            : result.Error!.Message);
    }

    private const string SsoHandler = "sso";
    private const string SignInNeeded = "Sign in with the prompt Teams just showed, then press Approve again. Azure DevOps records you as the author, so nothing is written without your sign-in.";

    // Approving writes to Azure DevOps as the approver, so the card action needs the approver's own single sign-on token (ADR 0020). When the
    // sign-in is still pending, the Agents SDK has already shown the sign-in prompt; nothing is approved and nothing falls back to a service identity.
    private async Task<bool> SignInApproverAsync(AsyncServiceScope scope, ITurnContext turnContext, ITurnState turnState, CancellationToken ct)
    {
        if (_ssoHandlers is null)
        {
            return false;
        }

        var token = await UserAuthorization.GetTurnTokenAsync(turnContext, SsoHandler, ct);
        if (!SsoToken.TryReadIdentity(token, out var oid, out var tid))
        {
            return false;
        }

        scope.ServiceProvider.GetRequiredService<IUserAssertionAccessor>().Current = new UserAssertion(token!, oid, tid);
        return true;
    }

    [GeneratedRegex(@"\bprepare\s+(?:the\s+)?(?:sprint\s+)?planning\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PreparePlanningPhrase();

    // The team this conversation is bound to, or null after answering the conversation with the onboarding card.
    private async Task<Team?> ResolveTeamAsync(ITurnContext turnContext, CancellationToken ct)
    {
        var tenant = TeamsActor.From(turnContext.Activity) is { IsSuccess: true } actor ? actor.Value.TenantId.ToString() : turnContext.Activity.Conversation?.TenantId;
        await using var scope = _scopes.CreateAsyncScope();
        var resolved = await scope.ServiceProvider.GetRequiredService<ITeamResolver>().ForConversationAsync(tenant, turnContext.Activity.Conversation?.Id, ct);
        if (resolved.IsSuccess)
        {
            return resolved.Value;
        }

        await SendOnboardingCardAsync(turnContext, resolved.Error!.Code == TeamErrors.AmbiguousBinding.Code, ct);
        return null;
    }

    // An unbound conversation gets one card and nothing else: no data, no commands. An Admin or the team's lead can bind it.
    private static async Task SendOnboardingCardAsync(ITurnContext turnContext, bool ambiguous, CancellationToken ct)
    {
        var text = ambiguous
            ? "This conversation is bound to more than one team. An administrator must fix the bindings."
            : "This conversation is not bound to a team yet. An administrator or a team lead can bind it by typing the team id and pressing Bind.";
        var card = new
        {
            type = "AdaptiveCard",
            version = "1.5",
            body = new object[]
            {
                new { type = "TextBlock", text = "Zava Scrum Agent", weight = "Bolder", size = "Medium" },
                new { type = "TextBlock", text, wrap = true },
                new { type = "Input.Text", id = "teamId", placeholder = "team id, for example team-payments" },
            },
            actions = new object[] { new { type = "Action.Execute", title = "Bind", verb = "bind-team" } },
        };
        var attachment = new Attachment { ContentType = "application/vnd.microsoft.card.adaptive", Content = JsonDocument.Parse(JsonSerializer.Serialize(card)).RootElement.Clone() };
        await turnContext.SendActivityAsync(MessageFactory.Attachment(attachment), ct);
    }

    private async Task<AdaptiveCardInvokeResponse> BindAsync(ITurnContext turnContext, object data, CancellationToken ct)
    {
        var raw = data is JsonElement e ? e.GetRawText() : JsonSerializer.Serialize(data);
        using var doc = JsonDocument.Parse(raw);
        var teamId = doc.RootElement.TryGetProperty("teamId", out var id) ? id.GetString()?.Trim() : null;
        var who = TeamsActor.From(turnContext.Activity);
        var conversation = turnContext.Activity.Conversation;
        if (string.IsNullOrWhiteSpace(teamId) || !who.IsSuccess || string.IsNullOrWhiteSpace(conversation?.Id))
        {
            return AdaptiveCardInvokeResponseFactory.BadRequest("Type a team id, and I need your Entra identity from Teams.");
        }

        await using var scope = _scopes.CreateAsyncScope();
        var caller = new CallerContext(who.Value.ObjectId, _identity.IsAdmin(who.Value.ObjectId));
        var isChannel = string.Equals(conversation.ConversationType, "channel", StringComparison.OrdinalIgnoreCase);
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new BindTeamsConversationCommand(caller, teamId, who.Value.TenantId.ToString(), null, conversation.Id, isChannel, BindingPurpose.Recap), ct);
        return AdaptiveCardInvokeResponseFactory.Message(result.IsSuccess ? $"Bound to {teamId}." : result.Error!.Message);
    }

    private async Task<bool> CanOperateAsync(AsyncServiceScope scope, ITurnContext turnContext, Team team, CancellationToken ct)
    {
        var actor = TeamsActor.From(turnContext.Activity);
        if (!actor.IsSuccess || actor.Value.TenantId != _identity.TenantId)
        {
            return false;
        }

        var caller = new CallerContext(actor.Value.ObjectId, _identity.IsAdmin(actor.Value.ObjectId));
        return (await scope.ServiceProvider.GetRequiredService<ITeamResolver>().ForCallerAsync(caller, team.Id, TeamPermission.Operate, ct)).IsSuccess;
    }

    private async Task RememberAsync(ITurnContext turnContext, CancellationToken ct)
    {
        var reference = turnContext.Activity.GetConversationReference();
        await _conversations.SaveAsync(reference.Conversation.Id, ProtocolJsonSerializer.ToJson(reference), ct);
    }
}
