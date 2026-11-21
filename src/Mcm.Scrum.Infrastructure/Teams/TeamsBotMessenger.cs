using System.Security.Claims;
using System.Text.Json;
using Microsoft.Agents.Authentication;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Infrastructure.Persistence;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Core.Models;
using Microsoft.Agents.Core.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mcm.Scrum.Infrastructure.Teams;

/// <summary>
/// Sends cards to Teams proactively through the Microsoft 365 Agents SDK, and always keeps the outbox copy so the portal
/// and the offline demo still work (SPEC 12). A Teams failure is logged and never fails the use case.
/// </summary>
/// <remarks>
/// DEMO: every card goes to every stored conversation (whoever messaged or installed the bot).
/// PROD: route by Entra object id, with one conversation per member and the Scrum Master.
/// </remarks>
public sealed class TeamsBotMessenger : ITeamsMessenger, IDisposable
{
    private readonly OutboxTeamsMessenger _outbox;
    private readonly IConversationStore _conversations;
    private readonly IChannelAdapter? _adapter;
    private readonly BotOptions _bot;
    private readonly ILogger<TeamsBotMessenger> _logger;
    private readonly string _activitiesPath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Initializes a new instance of the <see cref="TeamsBotMessenger"/> class.</summary>
    /// <param name="outbox">The offline outbox.</param>
    /// <param name="conversations">Stored conversation references.</param>
    /// <param name="bot">Bot options.</param>
    /// <param name="store">Store options.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="adapter">The Agents SDK adapter; absent when the bot is not configured.</param>
    public TeamsBotMessenger(
        OutboxTeamsMessenger outbox,
        IConversationStore conversations,
        IOptions<BotOptions> bot,
        IOptions<StoreOptions> store,
        ILogger<TeamsBotMessenger> logger,
        IChannelAdapter? adapter = null)
    {
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(store);
        _outbox = outbox;
        _conversations = conversations;
        _adapter = adapter;
        _bot = bot.Value;
        _logger = logger;
        _activitiesPath = Path.Combine(store.Value.DataPath, "bot-activities.json");
    }

    /// <inheritdoc />
    public async Task SendAsync(TeamsMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        await _outbox.SendAsync(message, ct);
        if (_adapter is null || !_bot.IsConfigured)
        {
            return;
        }

        var card = await FindCardJsonAsync(message, ct);
        foreach (var (key, json) in await _conversations.ListAsync(ct))
        {
            if (await HasActivityAsync(message.DedupeKey, key, ct))
            {
                continue;
            }

            await TryAsync(message, key, async () =>
            {
                string? id = null;
                await _adapter.ContinueConversationAsync(Identity(), Reference(json), async (turn, token) =>
                {
                    id = (await turn.SendActivityAsync(CardActivity(card), token))?.Id;
                }, ct);
                if (id is not null)
                {
                    await RememberAsync(message.DedupeKey, key, id, ct);
                }
            });
        }
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TeamsMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        await _outbox.UpdateAsync(message, ct);
        if (_adapter is null || !_bot.IsConfigured)
        {
            return;
        }

        var card = await FindCardJsonAsync(message, ct);
        var stored = (await _conversations.ListAsync(ct)).ToDictionary(c => c.Key, c => c.Value);
        foreach (var entry in (await ReadActivitiesAsync(ct)).Where(a => a.DedupeKey == message.DedupeKey))
        {
            if (!stored.TryGetValue(entry.ConversationKey, out var json))
            {
                continue;
            }

            await TryAsync(message, entry.ConversationKey, () => _adapter.ContinueConversationAsync(Identity(), Reference(json), async (turn, token) =>
            {
                var activity = CardActivity(card);
                activity.Id = entry.ActivityId;
                await turn.UpdateActivityAsync(activity, token);
            }, ct));
        }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private ClaimsIdentity Identity() => AgentClaims.CreateIdentity(_bot.AppId!);

    private static ConversationReference Reference(string json) =>
        ProtocolJsonSerializer.ToObject<ConversationReference>(json)!;

    private static IActivity CardActivity(string cardJson)
    {
        var attachment = new Attachment
        {
            ContentType = "application/vnd.microsoft.card.adaptive",
            Content = JsonDocument.Parse(cardJson).RootElement.Clone(),
        };
        return MessageFactory.Attachment(attachment);
    }

    private async Task<string> FindCardJsonAsync(TeamsMessage message, CancellationToken ct) =>
        (await _outbox.ListAsync(message.SessionId, ct)).First(c => c.Message.DedupeKey == message.DedupeKey).AdaptiveCardJson;

    private async Task TryAsync(TeamsMessage message, string conversationKey, Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Could not deliver {Kind} card {Key} to Teams conversation {Conversation}", message.Kind, message.DedupeKey, conversationKey);
        }
    }

    private async Task<bool> HasActivityAsync(string dedupeKey, string conversationKey, CancellationToken ct) =>
        (await ReadActivitiesAsync(ct)).Any(a => a.DedupeKey == dedupeKey && a.ConversationKey == conversationKey);

    private async Task<List<SentActivity>> ReadActivitiesAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            return await JsonFile.ReadAsync<List<SentActivity>>(_activitiesPath, () => [], ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task RememberAsync(string dedupeKey, string conversationKey, string activityId, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var all = await JsonFile.ReadAsync<List<SentActivity>>(_activitiesPath, () => [], ct);
            all.Add(new SentActivity(dedupeKey, conversationKey, activityId));
            await JsonFile.WriteAsync(_activitiesPath, all, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    private sealed record SentActivity(string DedupeKey, string ConversationKey, string ActivityId);
}
