using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.FollowUps.ApproveFollowUp;
using Zava.Scrum.Application.Features.FollowUps.RejectFollowUp;
using Zava.Scrum.Application.Features.Retrospectives.GetRetroBoard;
using Zava.Scrum.Application.Features.Retrospectives.NameRetroThemes;
using Zava.Scrum.Application.Features.Retrospectives.PrepareRetrospective;
using Zava.Scrum.Application.Features.Retrospectives.ReconcileRetroUpdate;
using Zava.Scrum.Application.Features.Standups.EndStandup;
using Zava.Scrum.Application.Features.Standups.GetNextSpeaker;
using Zava.Scrum.Application.Features.Standups.ListSessions;
using Zava.Scrum.Application.Features.Standups.RecordStandupEvent;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Voice;
using Zava.Scrum.Domain.Work;
using Microsoft.Extensions.DependencyInjection;

namespace Zava.Scrum.Application.Tests.Features.Retrospectives;

public sealed class RetrospectiveFlowTests : IAsyncDisposable
{
    private static readonly DateTimeOffset T = StandupHarness.Now;
    private readonly WriterSpy _writer = new();
    private readonly RecordingTeamsMessenger _messenger = new();
    private readonly InMemoryFollowUpRepository _followUps = new();
    private readonly InMemorySessionRepository _sessions = new();
    private ServiceProvider _sp = null!;

    private ISender Sender => _sp.GetRequiredService<ISender>();

    private async Task<Guid> StartAsync(string? modelReply = null)
    {
        var sprint = new SprintSnapshot("P\\Sprint 1", T, T, [new WorkItem(new WorkItemId(1), "Item 1", "User Story", "Active", null, "daniel", T, T, 5, null, null, [])]);
        var teams = new InMemoryTeamRepository();
        await teams.SaveAsync(new Team("t1", "Payments", "UTC", 15, new AdoSettings("o", "P", "t", "a"), new TeamBehaviours(), "Goal"), default);
        var members = new InMemoryMemberRepository();
        await members.ReplaceAsync("t1", [new Member("m1", "t1", "daniel", "Daniel Park", "daniel@x.com", "Daniel", 1, true), new Member("m2", "t1", "maya", "Maya Lin", "maya@x.com", "Maya", 2, true)], default);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<ITeamRepository>(teams);
        services.AddSingleton<IMemberRepository>(members);
        services.AddSingleton<IWorkItemReader>(new FakeWorkItemReader(Zava.Scrum.Domain.Common.Result<SprintSnapshot>.Success(sprint)));
        services.AddSingleton<IIterationReader>(new FakeIterationReader("P\\Sprint 2"));
        services.AddSingleton<ISessionRepository>(_sessions);
        services.AddSingleton<IFollowUpRepository>(_followUps);
        services.AddSingleton<ITeamsMessenger>(_messenger);
        services.AddSingleton<IDraftingAgent>(new FakeDraftingAgent(modelReply));
        services.AddSingleton<IWorkItemWriter>(_writer);
        services.AddSingleton<IMeetingBridge>(new FakeMeetingBridge());
        services.AddSingleton<IRetroSettings>(new FakeRetroSettings());
        services.AddSingleton<IClock>(new FixedClock(T));
        _sp = services.BuildServiceProvider();
        return (await Sender.Send(new PrepareRetrospectiveCommand("t1"))).Value;
    }

    private Task<Zava.Scrum.Domain.Common.Result> Record(Guid id, StandupEventType type, string member, string text, RetroCategory? category = null, string? owner = null) =>
        Sender.Send(new RecordStandupEventCommand(id, type, member, text, null, category, owner));

    [Fact]
    public async Task Prepare_Retrospective_StoresSessionWithKindAndIsListed()
    {
        var id = await StartAsync();

        var session = (await _sessions.GetAsync(id, default))!;
        Assert.Equal(CeremonyKind.Retrospective, session.Kind);
        Assert.Equal(2, session.PreBrief!.Members.Count);
        Assert.Equal(CeremonyKind.Retrospective, Assert.Single((await Sender.Send(new ListSessionsQuery("t1"))).Value).Kind);
        Assert.Empty(_messenger.Sent);
    }

    [Fact]
    public async Task GetNextSpeaker_Retrospective_UsesTheRetroAgenda()
    {
        var id = await StartAsync();
        var session = (await _sessions.GetAsync(id, default))!;
        foreach (var m in session.PreBrief!.Members)
        {
            session.CompleteTurn(m.MemberId);
        }

        await _sessions.SaveAsync(session, default);

        var next = (await Sender.Send(new GetNextSpeakerQuery(id))).Value;

        Assert.Equal(SpeakReason.RetroThemes, next.Announcement);
        Assert.Null(next.MemberId);
    }

    [Fact]
    public async Task Reconcile_UnclearNote_AsksTheClarifyingQuestionAndRecordsNothingItself()
    {
        var id = await StartAsync();

        var result = (await Sender.Send(new ReconcileRetroUpdateQuery(id, "m1", "We should stop long meetings. The release process is painful."))).Value;

        Assert.Equal(RetroItemClassifier.ClarifyingQuestion, result.SuggestedQuestion);
        Assert.Equal([RetroCategory.Stop, null], result.Events.Select(e => e.Category));
        Assert.Empty((await _sessions.GetAsync(id, default))!.Events);
    }

    [Fact]
    public async Task NameThemes_WithoutModel_UsesKeywordsAndIsIdempotent()
    {
        var id = await StartAsync();
        await Record(id, StandupEventType.RetroItem, "m1", "Deployments take too long", RetroCategory.Stop);
        await Record(id, StandupEventType.RetroItem, "m2", "Failed deployments block everyone", RetroCategory.Stop);

        await Sender.Send(new NameRetroThemesCommand(id));
        await Sender.Send(new NameRetroThemesCommand(id));

        var themes = (await _sessions.GetAsync(id, default))!.Events.Where(e => e.Type == StandupEventType.Theme).ToList();
        Assert.Equal("Deployment", Assert.Single(themes).Text);
    }

    [Fact]
    public async Task NameThemes_ModelNames_AreUsedWhenTheCountMatches()
    {
        var id = await StartAsync("{\"names\":[\"Slow releases\"]}");
        await Record(id, StandupEventType.RetroItem, "m1", "Deployments take too long", RetroCategory.Stop);
        await Record(id, StandupEventType.RetroItem, "m2", "Failed deployments block everyone", RetroCategory.Stop);

        await Sender.Send(new NameRetroThemesCommand(id));

        Assert.Equal("Slow releases", Assert.Single((await _sessions.GetAsync(id, default))!.Events, e => e.Type == StandupEventType.Theme).Text);
    }

    [Fact]
    public async Task NameThemes_ModelReplyWithWrongCount_FallsBackToKeywords()
    {
        var id = await StartAsync("{\"names\":[\"A\",\"B\"]}");
        await Record(id, StandupEventType.RetroItem, "m1", "Deployments take too long", RetroCategory.Stop);
        await Record(id, StandupEventType.RetroItem, "m2", "Failed deployments block everyone", RetroCategory.Stop);

        await Sender.Send(new NameRetroThemesCommand(id));

        Assert.Equal("Deployment", Assert.Single((await _sessions.GetAsync(id, default))!.Events, e => e.Type == StandupEventType.Theme).Text);
    }

    [Fact]
    public async Task End_Retrospective_CreatesAScrumMasterFollowUpWithTasksAndWritesNothingYet()
    {
        var id = await StartAsync();
        await Record(id, StandupEventType.RetroItem, "m1", "Deployments take too long", RetroCategory.Stop);
        await Record(id, StandupEventType.ActionItem, "m1", "Automate the deployment checklist", owner: "m2");

        await Sender.Send(new EndStandupCommand(id));
        await Sender.Send(new EndStandupCommand(id));

        var followUp = Assert.Single(await _followUps.ListBySessionAsync(id, default));
        Assert.True(followUp.ForScrumMaster);
        Assert.Equal(FollowUpKind.RetroActions, followUp.Kind);
        Assert.Equal(CeremonyKind.Retrospective, followUp.Ceremony);
        var change = Assert.Single(followUp.Changes);
        Assert.Equal(ChangeOp.CreateWorkItem, change.Op);
        Assert.Equal("P\\Sprint 2", change.New!.IterationPath);
        Assert.Equal("maya@x.com", change.New.AssignedTo);
        Assert.Equal(["retro-action"], change.New.Tags);
        Assert.Contains(_messenger.Sent, m => m.Kind == CardKind.RetroActions);
        Assert.Contains(_messenger.Sent, m => m.Kind == CardKind.Recap && m.Route == CardRoute.Recap);
        Assert.Empty(_writer.Written);
        Assert.Equal(2, _messenger.Sent.Count);
    }

    [Fact]
    public async Task Approve_RetroFollowUp_CreatesTheTaskWithRetrospectiveAttribution()
    {
        var id = await StartAsync();
        await Record(id, StandupEventType.ActionItem, "m1", "Automate the deployment checklist", owner: "m2");
        await Sender.Send(new EndStandupCommand(id));
        var followUp = Assert.Single(await _followUps.ListBySessionAsync(id, default));

        await Sender.Send(new ApproveFollowUpCommand(followUp.Id, "Sam", ApprovalChannel.Portal));

        var written = Assert.Single(_writer.Written);
        Assert.Equal(ChangeOp.CreateWorkItem, written.Op);
        Assert.Equal("Approved by Sam in the portal, proposed by MCM Scrum Agent from the 2026-01-08 retrospective.", written.Attribution);
    }

    [Fact]
    public async Task Reject_RetroFollowUp_WritesNothing()
    {
        var id = await StartAsync();
        await Record(id, StandupEventType.ActionItem, "m1", "Automate the deployment checklist", owner: "m2");
        await Sender.Send(new EndStandupCommand(id));
        var followUp = Assert.Single(await _followUps.ListBySessionAsync(id, default));

        await Sender.Send(new RejectFollowUpCommand(followUp.Id, "Sam"));

        Assert.Empty(_writer.Written);
    }

    [Fact]
    public async Task GetRetroBoard_ReturnsColumnsTitledForTheFormat()
    {
        var id = await StartAsync();
        await Record(id, StandupEventType.RetroItem, "m1", "Deployments take too long", RetroCategory.Stop);

        var board = (await Sender.Send(new GetRetroBoardQuery(id))).Value;

        Assert.Equal(["Went well", "Do better", "Missing"], board.Columns.Select(c => c.Label));
        Assert.Equal("Deployments take too long", Assert.Single(board.Columns[1].Notes).Text);
        Assert.Equal("Round", board.Phase);
    }

    public ValueTask DisposeAsync() => _sp.DisposeAsync();
}
