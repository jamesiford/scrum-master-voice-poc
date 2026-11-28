using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Zava.Scrum.Application;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.FollowUps.GetFollowUp;
using Zava.Scrum.Application.Features.FollowUps.ListFollowUps;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Infrastructure.Teams;
using Zava.Scrum.Infrastructure.Persistence;

namespace Zava.Scrum.Infrastructure.Tests.Persistence;

/// <summary>Actual file adapters and real mediator coverage of item isolation, retries, reset generations and migration.</summary>
public sealed class TeamRecordStorageTests : IDisposable
{
    private static readonly string[] FirstActivityKey = ["same-card", "first"];
    private readonly string _path = Path.Combine(Path.GetTempPath(), "zava-record-test-" + Guid.NewGuid().ToString("N"));
    private readonly IOptions<StoreOptions> _options;
    private readonly FileCollectionStore _legacy;
    private readonly FileDemoStateStore _publication;
    private readonly FileTeamRecordStore _items;
    private readonly RecordCollectionStore _records;
    private readonly DateTimeOffset _now = DateTimeOffset.Parse("2026-11-27T20:00:00Z", System.Globalization.CultureInfo.InvariantCulture);

    public TeamRecordStorageTests()
    {
        _options = Options.Create(new StoreOptions { DataPath = _path });
        _legacy = new FileCollectionStore(_options);
        _publication = new FileDemoStateStore(_options);
        _items = new FileTeamRecordStore(_options);
        _records = new RecordCollectionStore(new DemoPublicationCollectionStore(_legacy, _publication), _items, _publication);
    }

    [Fact]
    public async Task Query_RealMediator_SeparatesTeamsSessionsAndStatusesAfterRestart()
    {
        using var repository = new JsonFileFollowUpRepository(_records);
        var session = Guid.NewGuid();
        var own = FollowUp.Create(session, "one", session, "daniel", FollowUpKind.Followup, "Own", [], new DateOnly(2026, 11, 27), _now);
        var other = FollowUp.Create(Guid.NewGuid(), "two", Guid.NewGuid(), "maya", FollowUpKind.Followup, "Other", [], new DateOnly(2026, 11, 27), _now);
        await repository.SaveAsync(own, default);
        await repository.SaveAsync(other, default);
        var services = new ServiceCollection().AddLogging().AddApplication();
        services.AddSingleton<IFollowUpRepository>(repository);
        await using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var listed = await sender.Send(new ListFollowUpsQuery("one", FollowUpStatus.Pending));
        var found = await sender.Send(new GetFollowUpQuery(own.Id));

        Assert.True(listed.IsSuccess);
        Assert.Equal(own.Id, Assert.Single(listed.Value).Id);
        Assert.True(found.IsSuccess);
        Assert.Equal("Own", found.Value.Message);
        Assert.Equal(own.Id, Assert.Single(await repository.ListBySessionAsync(session, default)).Id);
        var restarted = new FileTeamRecordStore(_options);
        Assert.Equal(2, (await restarted.QueryAsync("followups.json", null, null, null, null, default)).Count);
    }

    [Fact]
    public async Task Send_ConcurrentDuplicateAndUpdate_PreservesCardIdentityAndOtherTeams()
    {
        using var outbox = new OutboxTeamsMessenger(_records, new FixedClock(_now));
        var message = Message("one", Guid.NewGuid(), "same-key");
        await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => outbox.SendAsync(message, default)));
        await outbox.SendAsync(Message("two", Guid.NewGuid(), "same-key"), default);
        var before = Assert.Single(await outbox.ListAsync(message.SessionId, default));

        await outbox.UpdateAsync(message with { Text = "Approved", Kind = CardKind.FollowUpResult }, default);

        var after = Assert.Single(await outbox.ListAsync(message.SessionId, default));
        Assert.Equal(before.Id, after.Id);
        Assert.Equal(before.SentAt, after.SentAt);
        Assert.Equal("Approved", after.Message.Text);
        Assert.Equal(2, (await outbox.ListAsync(null, default)).Count);
        Assert.Equal(2, Directory.EnumerateFiles(Path.Combine(_path, "team-records"), "*.json", SearchOption.AllDirectories).Count());
    }

    [Fact]
    public async Task Migration_ReplayedAfterPartialCopy_PreservesNewerItemsAndLegacyExport()
    {
        var session = Guid.NewGuid();
        var followup = FollowUp.Create(Guid.NewGuid(), "one", session, "daniel", FollowUpKind.Followup, "Legacy", [], new DateOnly(2026, 11, 27), _now).ToSnapshot();
        await _legacy.WriteAsync("followups.json", new[] { followup }, default);
        var card = new { Id = Guid.NewGuid(), SentAt = _now, Message = Message("one", session, "legacy-card"), AdaptiveCardJson = "{}" };
        await _legacy.WriteAsync("cards.json", new[] { card }, default);
        await _legacy.WriteAsync("bot-activities.json", new[] { new { DedupeKey = "legacy-card", ConversationKey = "chat", ActivityId = "activity" } }, default);
        var newer = RecordCollectionStore.Describe("followups.json", JsonSerializer.SerializeToElement(followup with { Message = "Newer" }, JsonFile.SerializerOptions), 0);
        await _items.CreateAsync(newer, default);
        var migration = new TeamRecordMigration(new DemoPublicationCollectionStore(_legacy, _publication), _items, _publication,
            new FileSessionItemStore(_options), NullLogger<TeamRecordMigration>.Instance);

        Assert.Equal(2, await migration.RunAsync(default));
        Assert.Equal(0, await migration.RunAsync(default));

        Assert.Equal("Newer", Assert.Single(await _records.QueryAsync<FollowUpSnapshot>("followups.json", "one", session, null, null, default)).Message);
        var activity = Assert.Single(await _items.QueryAsync("bot-activities.json", "one", session.ToString(), "Sent", null, default));
        Assert.Equal("one", activity.Json.GetProperty("TeamId").GetString());
        Assert.Single(await _legacy.ReadAsync<List<JsonElement>>("followups.json", () => [], default));
        Assert.Empty((await _publication.ReadAsync(default)).Collections);
    }

    [Fact]
    public async Task Migration_AuthoritativeEmptyOverride_DoesNotResurrectLegacyFollowUps()
    {
        var followup = FollowUp.Create(Guid.NewGuid(), "one", Guid.NewGuid(), null, FollowUpKind.Followup,
            "Hidden legacy", [], new DateOnly(2026, 11, 27), _now).ToSnapshot();
        await _legacy.WriteAsync("followups.json", new[] { followup }, default);
        await _publication.UpdateAsync(state =>
        {
            state.Collections["followups.json"] = JsonSerializer.SerializeToElement(Array.Empty<FollowUpSnapshot>());
            return true;
        }, default);
        var migration = new TeamRecordMigration(new DemoPublicationCollectionStore(_legacy, _publication), _items, _publication,
            new FileSessionItemStore(_options), NullLogger<TeamRecordMigration>.Instance);

        Assert.Equal(0, await migration.RunAsync(default));

        Assert.Empty(await _records.QueryAsync<FollowUpSnapshot>("followups.json", "one", null, null, null, default));
        Assert.Single(await _legacy.ReadAsync<List<FollowUpSnapshot>>("followups.json", () => [], default));
    }

    [Fact]
    public async Task Save_ActivityRetries_UsesDistinctConversationIdsAndPreservesFirstDelivery()
    {
        var session = Guid.NewGuid();
        var activity = JsonSerializer.SerializeToElement(new
        {
            TeamId = "one",
            SessionId = session,
            DedupeKey = "same-card",
            ConversationKey = "first",
            ActivityId = "delivered",
            CreatedAt = _now
        });
        await _records.SaveRecordAsync("bot-activities.json", activity, true, default);
        var retry = JsonSerializer.SerializeToElement(new
        {
            TeamId = "one",
            SessionId = session,
            DedupeKey = "same-card",
            ConversationKey = "first",
            ActivityId = "must-not-replace",
            CreatedAt = _now
        });
        await _records.SaveRecordAsync("bot-activities.json", retry, true, default);
        await _records.SaveRecordAsync("bot-activities.json", JsonSerializer.SerializeToElement(new
        {
            TeamId = "one",
            SessionId = session,
            DedupeKey = "same-card",
            ConversationKey = "second",
            ActivityId = "other-delivery",
            CreatedAt = _now
        }), true, default);

        var first = Assert.Single(await _records.QueryAsync<JsonElement>("bot-activities.json", "one", session, "Sent",
            JsonSerializer.Serialize(FirstActivityKey), default));
        Assert.Equal("delivered", first.GetProperty("ActivityId").GetString());
        Assert.Equal(2, (await _records.QueryAsync<JsonElement>("bot-activities.json", "one", session, null, null, default)).Count);
    }

    [Fact]
    public async Task Reset_GenerationChanged_HidesOnlySelectedTeamAndRefusesOldWrites()
    {
        using var outbox = new OutboxTeamsMessenger(_records, new FixedClock(_now));
        var message = Message("one", Guid.NewGuid(), "old");
        await outbox.SendAsync(message, default);
        await outbox.SendAsync(Message("two", Guid.NewGuid(), "other"), default);
        await _publication.UpdateAsync(state =>
        {
            state.RecordStorageVersion = 1;
            state.RecordGenerations["one"] = 1;
            state.RecordCutoffs["one"] = _now;
            state.Epoch++;
            return true;
        }, default);

        Assert.Equal("two", Assert.Single(await outbox.ListAsync(null, default)).Message.TeamId);
        await Assert.ThrowsAsync<Zava.Scrum.Application.Features.Demo.ReseedDemo.DemoResetConflictException>(() => outbox.SendAsync(message, default));
        using var fresh = new OutboxTeamsMessenger(_records, new FixedClock(_now.AddSeconds(1)));
        await fresh.SendAsync(Message("one", Guid.NewGuid(), "fresh"), default);
        Assert.Equal(2, (await fresh.ListAsync(null, default)).Count);
        Assert.Equal(3, (await _items.QueryAsync("cards.json", null, null, null, null, default)).Count);
    }

    public void Dispose()
    {
        _items.Dispose();
        if (Directory.Exists(_path)) Directory.Delete(_path, recursive: true);
    }

    private static TeamsMessage Message(string team, Guid session, string key) =>
        new(CardKind.FollowUp, team, session, key, "Title", "Text", []);

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
