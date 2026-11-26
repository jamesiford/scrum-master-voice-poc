using Microsoft.Extensions.DependencyInjection;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Standups.EndStandup;
using Zava.Scrum.Application.Features.Standups.StartStandup;
using Zava.Scrum.Application.Features.Teams.ReceiveMeetingEvent;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Tests.Features.Teams;

/// <summary>System meeting events exercise the real mediator, resolver and end notifications with fake ports only.</summary>
public sealed class MeetingEventTests
{
    private const string Conversation = "19:meeting-one@thread.v2";
    private const string MeetingId = "opaque-channel-data-id";
    private const string JoinUrl = "https://teams.microsoft.com/l/meetup-join/19%3ameeting-one%40thread.v2/0?context=%7b%22Tid%22%3a%2211111111-1111-1111-1111-111111111111%22%7d";

    [Fact]
    public async Task Receive_StartTwice_RecordsMetadataWithoutStartingAnotherCeremonyOrSendingCards()
    {
        await using var h = await CreateAsync();
        var prepared = StandupSession.Prepare(Guid.NewGuid(), "t1", StandupHarness.Now, null);
        await h.Sessions.SaveAsync(prepared, default);

        Assert.True((await h.Sender.Send(Event(MeetingEventKind.Started))).IsSuccess);
        Assert.True((await h.Sender.Send(Event(MeetingEventKind.Started))).IsSuccess);

        var session = await h.LoadAsync();
        Assert.Equal(StandupStatus.Live, session.Status);
        Assert.Equal(MeetingId, session.MeetingLifecycle!.MeetingId);
        Assert.Equal(Conversation, session.MeetingLifecycle.ConversationId);
        Assert.Equal(StandupHarness.Now, session.MeetingLifecycle.StartedAt);
        Assert.Null(session.MeetingLifecycle.EndedAt);
        Assert.Equal(StandupStatus.Prepared, prepared.Status);
        Assert.Empty(h.Get<RecordingTeamsMessenger>().Sent);
        Assert.Equal(session.MeetingLifecycle, StandupSession.Restore(session.ToSnapshot()).MeetingLifecycle);
    }

    [Fact]
    public async Task Receive_StartForUnpreparedOrUnknownMeeting_DoesNotCreateOrStartACeremony()
    {
        await using var h = await CreateAsync(start: false);

        Assert.True((await h.Sender.Send(Event(MeetingEventKind.Started))).IsSuccess);
        Assert.True((await h.Sender.Send(Event(MeetingEventKind.Ended))).IsSuccess);

        Assert.Equal(StandupStatus.Prepared, (await h.LoadAsync()).Status);
        Assert.Null((await h.LoadAsync()).MeetingLifecycle);
        Assert.Single(await h.Sessions.ListAsync(default));
        Assert.Empty(h.Get<RecordingTeamsMessenger>().Sent);
    }

    [Fact]
    public async Task Receive_MeetingStartsBeforeHumanStartsPreparedCeremony_MatchingEndFinishesOnce()
    {
        // Arrange
        await using var h = await CreateAsync(start: false);

        // Act: Teams starts the meeting, but only the presenter's normal command starts the ceremony.
        Assert.True((await h.Sender.Send(Event(MeetingEventKind.Started))).IsSuccess);
        Assert.Equal(StandupStatus.Prepared, (await h.LoadAsync()).Status);
        Assert.Null((await h.LoadAsync()).MeetingLifecycle);
        Assert.Empty(h.Get<RecordingTeamsMessenger>().Sent);
        Assert.True((await h.Sender.Send(new StartStandupCommand(h.SessionId, "acs", JoinUrl))).IsSuccess);
        (await h.LoadAsync()).MarkAbsent("m-marcus", StandupHarness.Now);
        Assert.True((await h.Sender.Send(Event(MeetingEventKind.Ended))).IsSuccess);
        Assert.True((await h.Sender.Send(Event(MeetingEventKind.Ended))).IsSuccess);
        Assert.True((await h.Sender.Send(new EndStandupCommand(h.SessionId))).IsSuccess);

        // Assert
        var session = await h.LoadAsync();
        Assert.Equal(StandupStatus.Ended, session.Status);
        Assert.Equal(MeetingId, session.MeetingLifecycle!.MeetingId);
        Assert.Equal(Conversation, session.MeetingLifecycle.ConversationId);
        Assert.Null(session.MeetingLifecycle.StartedAt);
        Assert.Equal(StandupHarness.Now, session.MeetingLifecycle.EndedAt);
        Assert.Single(await h.Sessions.ListAsync(default));
        Assert.Single(h.Get<RecordingBridge>().Left);
        Assert.Single(h.Get<RecordingTeamsMessenger>().Sent, c => c.Kind == CardKind.Recap);
        Assert.Single(h.Get<RecordingTeamsMessenger>().Sent, c => c.Kind == CardKind.Absent && c.MemberId == "m-marcus");
        Assert.NotEmpty(await h.Get<IFollowUpRepository>().ListBySessionAsync(h.SessionId, default));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Receive_EndWithOrWithoutStart_EndsOnceAndHumanEndRemainsIdempotent(bool deliverStart)
    {
        await using var h = await CreateAsync();
        (await h.LoadAsync()).MarkAbsent("m-marcus", StandupHarness.Now);
        if (deliverStart)
        {
            await h.Sender.Send(Event(MeetingEventKind.Started));
        }

        Assert.True((await h.Sender.Send(Event(MeetingEventKind.Ended))).IsSuccess);
        await h.Sender.Send(Event(MeetingEventKind.Ended));
        await h.Sender.Send(new EndStandupCommand(h.SessionId));

        var session = await h.LoadAsync();
        Assert.Equal(StandupStatus.Ended, session.Status);
        Assert.Equal(StandupHarness.Now, session.MeetingLifecycle!.EndedAt);
        Assert.Single(h.Get<RecordingBridge>().Left);
        Assert.Single(h.Get<RecordingTeamsMessenger>().Sent, c => c.Kind == CardKind.Recap);
        Assert.Single(h.Get<RecordingTeamsMessenger>().Sent, c => c.Kind == CardKind.Absent && c.MemberId == "m-marcus");
        Assert.NotEmpty(await h.Get<IFollowUpRepository>().ListBySessionAsync(h.SessionId, default));
    }

    [Fact]
    public async Task Receive_AfterHumanEnd_DoesNotRepeatConsequencesOrRecordALateStart()
    {
        await using var h = await CreateAsync();
        await h.Sender.Send(new EndStandupCommand(h.SessionId));
        var endedAt = (await h.LoadAsync()).EndedAt;
        var cards = h.Get<RecordingTeamsMessenger>().Sent.Count;

        await h.Sender.Send(Event(MeetingEventKind.Ended));
        await h.Sender.Send(Event(MeetingEventKind.Started));

        Assert.Equal(endedAt, (await h.LoadAsync()).EndedAt);
        Assert.Null((await h.LoadAsync()).MeetingLifecycle);
        Assert.Equal(cards, h.Get<RecordingTeamsMessenger>().Sent.Count);
        Assert.Single(h.Get<RecordingBridge>().Left);
    }

    [Theory]
    [InlineData("other-tenant")]
    [InlineData("unbound")]
    [InlineData("ambiguous-binding")]
    [InlineData("cross-team")]
    [InlineData("other-meeting")]
    [InlineData("other-id")]
    [InlineData("other-bound-chat")]
    [InlineData("missing-id")]
    public async Task Receive_UnrelatedOrUntrustedScope_DoesNotAffectTheSession(string scenario)
    {
        await using var h = await CreateAsync();
        await h.Sender.Send(Event(MeetingEventKind.Started));
        var request = Event(MeetingEventKind.Ended);
        switch (scenario)
        {
            case "other-tenant":
                request = request with { TenantId = Guid.NewGuid().ToString() };
                break;
            case "unbound":
                request = request with { ConversationId = "unbound" };
                break;
            case "ambiguous-binding":
                await BindAsync(h, "team-b", Conversation);
                break;
            case "cross-team":
                await BindAsync(h, "team-b", "team-b-chat");
                request = request with { ConversationId = "team-b-chat" };
                break;
            case "other-meeting":
                request = request with { JoinUrl = "https://teams.microsoft.com/l/meetup-join/other/0" };
                break;
            case "other-id":
                request = request with { MeetingId = "payload-id-not-channel-data-id" };
                break;
            case "other-bound-chat":
                var team = (await h.Get<ITeamRepository>().GetAsync("t1", default))!;
                await h.Get<ITeamRepository>().SaveAsync(team with
                {
                    TeamsBindings = [.. team.Bindings, Binding("other-chat")],
                }, default);
                request = request with { ConversationId = "other-chat" };
                break;
            case "missing-id":
                request = request with { MeetingId = null };
                break;
        }

        await h.Sender.Send(request);

        Assert.Equal(StandupStatus.Live, (await h.LoadAsync()).Status);
        Assert.Null((await h.LoadAsync()).MeetingLifecycle!.EndedAt);
        Assert.Empty(h.Get<RecordingBridge>().Left);
        Assert.Empty(h.Get<RecordingTeamsMessenger>().Sent);
    }

    [Fact]
    public async Task Receive_MultipleActiveSessionsForOneMeeting_RefusesToGuess()
    {
        await using var h = await CreateAsync();
        var other = StandupSession.Prepare(Guid.NewGuid(), "t1", StandupHarness.Now, null);
        other.Start("acs", JoinUrl, 15, StandupHarness.Now);
        await h.Sessions.SaveAsync(other, default);

        var result = await h.Sender.Send(Event(MeetingEventKind.Ended));

        Assert.Equal("meeting.ambiguous_session", result.Error!.Code);
        Assert.Equal(StandupStatus.Live, other.Status);
        Assert.Equal(StandupStatus.Live, (await h.LoadAsync()).Status);
        Assert.Empty(h.Get<RecordingBridge>().Left);
    }

    [Fact]
    public async Task Receive_EndFromAnotherBoundChatBeforeStart_DoesNotClaimOrEndTheMeeting()
    {
        await using var h = await CreateAsync();
        var team = (await h.Get<ITeamRepository>().GetAsync("t1", default))!;
        await h.Get<ITeamRepository>().SaveAsync(team with { TeamsBindings = [.. team.Bindings, Binding("other-chat")] }, default);

        var result = await h.Sender.Send(Event(MeetingEventKind.Ended) with { ConversationId = "other-chat" });

        Assert.True(result.IsSuccess);
        Assert.Equal(StandupStatus.Live, (await h.LoadAsync()).Status);
        Assert.Null((await h.LoadAsync()).MeetingLifecycle);
        Assert.Empty(h.Get<RecordingBridge>().Left);
    }

    [Fact]
    public async Task Receive_EndRetrospective_UsesItsOwnOutcomeAndLeavesOnce()
    {
        await using var h = await CreateAsync();
        var session = await h.LoadAsync();
        session = StandupSession.Restore(session.ToSnapshot() with { Kind = CeremonyKind.Retrospective });
        session.RecordEvent(StandupEventType.RetroItem, "m-priya", [], "Keep pairing", StandupHarness.Now, RetroCategory.Continue);
        await h.Sessions.SaveAsync(session, default);

        await h.Sender.Send(Event(MeetingEventKind.Ended));
        await h.Sender.Send(Event(MeetingEventKind.Ended));

        Assert.Equal(StandupStatus.Ended, session.Status);
        Assert.Single(h.Get<RecordingBridge>().Left);
        Assert.Single(h.Get<RecordingTeamsMessenger>().Sent, c => c.Title == "Retrospective summary");
        Assert.Empty(await h.Get<IFollowUpRepository>().ListBySessionAsync(h.SessionId, default));
    }

    [Fact]
    public async Task Receive_EndWhenHangupCancelsDeliveryToken_CompletesRecapAndFollowUps()
    {
        using var cancellation = new CancellationTokenSource();
        var bridge = new RecordingBridge(cancellation);
        var messenger = new CancellationAwareMessenger();
        await using var h = await CreateAsync(configure: services =>
        {
            services.AddSingleton<IMeetingBridge>(bridge);
            services.AddSingleton<ITeamsMessenger>(messenger);
        });
        (await h.LoadAsync()).MarkAbsent("m-marcus", StandupHarness.Now);

        Assert.True((await h.Sender.Send(Event(MeetingEventKind.Ended), cancellation.Token)).IsSuccess);

        Assert.True(cancellation.IsCancellationRequested);
        Assert.Single(bridge.Left);
        Assert.Single(messenger.Sent, c => c.Kind == CardKind.Recap);
        Assert.Single(messenger.Sent, c => c.Kind == CardKind.Absent);
        Assert.All(bridge.Tokens, token => Assert.False(token.CanBeCanceled));
    }

    private static ReceiveMeetingEventCommand Event(MeetingEventKind kind) =>
        new(Who.Tenant.ToString(), Conversation, MeetingId, JoinUrl, kind);

    private static TeamsBinding Binding(string conversation) =>
        new(Who.Tenant.ToString(), null, null, conversation, BindingPurpose.Recap);

    private static async Task<StandupHarness> CreateAsync(bool start = true, Action<IServiceCollection>? configure = null)
    {
        var bridge = new RecordingBridge();
        var messenger = new RecordingTeamsMessenger();
        var h = await StandupHarness.CreateAsync(configure: services =>
        {
            services.AddSingleton(bridge);
            services.AddSingleton<IMeetingBridge>(bridge);
            services.AddSingleton(messenger);
            services.AddSingleton<ITeamsMessenger>(messenger);
            services.AddSingleton<IIterationReader>(new FakeIterationReader(null));
            configure?.Invoke(services);
        });
        var team = (await h.Get<ITeamRepository>().GetAsync("t1", default))!;
        await h.Get<ITeamRepository>().SaveAsync(team with { TeamsBindings = [Binding(Conversation)] }, default);
        var session = await h.LoadAsync();
        session.ClearDomainEvents();
        if (start)
        {
            Assert.True(session.Start("acs", JoinUrl, 15, StandupHarness.Now).IsSuccess);
        }

        return h;
    }

    private static Task BindAsync(StandupHarness h, string teamId, string conversation) =>
        h.Get<ITeamRepository>().SaveAsync(
            new Team(teamId, teamId, "UTC", 15, new AdoSettings("o", "p", "t", "a"), new TeamBehaviours(), "goal",
                TeamsBindings: [Binding(conversation)]), default);

    private sealed class RecordingBridge(CancellationTokenSource? caller = null) : IMeetingBridge
    {
        public List<Guid> Left { get; } = [];

        public List<CancellationToken> Tokens { get; } = [];

        public Task<Result> JoinAsync(MeetingJoinRequest request, CancellationToken ct) => Task.FromResult(Result.Success());

        public Task LeaveAsync(Guid sessionId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Left.Add(sessionId);
            Tokens.Add(ct);
            caller?.Cancel();
            return Task.CompletedTask;
        }
    }

    private sealed class CancellationAwareMessenger : ITeamsMessenger
    {
        public List<TeamsMessage> Sent { get; } = [];

        public Task UpdateAsync(TeamsMessage message, CancellationToken ct) => Task.CompletedTask;

        public Task SendAsync(TeamsMessage message, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }
}
