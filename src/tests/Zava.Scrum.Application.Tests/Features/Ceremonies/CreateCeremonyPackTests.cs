using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Ceremonies.CreateCeremonyPack;
using Zava.Scrum.Application.Features.FollowUps.ApproveFollowUp;
using Zava.Scrum.Application.Features.FollowUps.RejectFollowUp;
using Zava.Scrum.Application.Features.Standups.ListSessions;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;
using Microsoft.Extensions.DependencyInjection;

namespace Zava.Scrum.Application.Tests.Features.Ceremonies;

public sealed class CreateCeremonyPackTests : IAsyncDisposable
{
    private static readonly DateTimeOffset T = StandupHarness.Now;
    private readonly WriterSpy _writer = new();
    private readonly RecordingTeamsMessenger _messenger = new();
    private readonly InMemoryFollowUpRepository _followUps = new();
    private readonly InMemorySessionRepository _sessions = new();
    private ServiceProvider _sp = null!;

    private ISender Sender => _sp.GetRequiredService<ISender>();

    private static WorkItem Item(int id, string type, string state, string? owner, double? points) =>
        new(new WorkItemId(id), $"Item {id}", type, state, null, owner, T, T, points, null, null, []);

    private async Task StartAsync(string? modelReply = null)
    {
        var sprint = new SprintSnapshot("P\\Sprint 1", T, T, [Item(1, "User Story", "Closed", "daniel", 8), Item(2, "User Story", "Active", "daniel", 5), Item(3, "User Story", "New", null, 3)]);
        var teams = new InMemoryTeamRepository();
        await teams.SaveAsync(new Team("t1", "Payments", "UTC", 15, new AdoSettings("o", "P", "t", "a"), new TeamBehaviours(), "Goal"), default);
        var members = new InMemoryMemberRepository();
        await members.ReplaceAsync("t1", [new Member("m1", "t1", "daniel", "Daniel Park", "daniel@x.com", "Daniel", 1, true)], default);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<ITeamRepository>(teams);
        services.AddSingleton<IMemberRepository>(members);
        services.AddSingleton<IWorkItemReader>(new FakeWorkItemReader(Result<SprintSnapshot>.Success(sprint)));
        services.AddSingleton<IIterationReader>(new FakeIterationReader("P\\Sprint 2"));
        services.AddSingleton<ISessionRepository>(_sessions);
        services.AddSingleton<IFollowUpRepository>(_followUps);
        services.AddSingleton<ITeamsMessenger>(_messenger);
        services.AddSingleton<IDraftingAgent>(new FakeDraftingAgent(modelReply));
        services.AddSingleton<IWorkItemWriter>(_writer);
        services.AddSingleton<IClock>(new FixedClock(T));
        _sp = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Create_ReviewPack_SendsOneScrumMasterCardAndWritesNothing()
    {
        await StartAsync();

        var id = (await Sender.Send(new CreateCeremonyPackCommand("t1", CeremonyKind.SprintReview))).Value;

        var followUp = (await _followUps.GetAsync(id, default))!;
        Assert.True(followUp.ForScrumMaster);
        Assert.Equal(FollowUpKind.ReviewPack, followUp.Kind);
        Assert.Equal(CeremonyKind.SprintReview, followUp.Ceremony);
        Assert.Equal(FollowUpStatus.Sent, followUp.Status);
        Assert.Contains(followUp.Changes, c => c.Op == ChangeOp.Comment && c.WorkItemId == 1);
        Assert.Equal(CardKind.ReviewPack, Assert.Single(_messenger.Sent).Kind);
        Assert.Empty(_writer.Written);
    }

    [Fact]
    public async Task Create_SprintPlanning_IsInvalidBecausePlanningHasItsOwnPack()
    {
        await StartAsync();

        var result = await Sender.Send(new CreateCeremonyPackCommand("t1", CeremonyKind.SprintPlanning));

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task Create_Pack_DoesNotAppearInTheStandupSessionList()
    {
        await StartAsync();
        await Sender.Send(new CreateCeremonyPackCommand("t1", CeremonyKind.Retrospective));

        var list = await Sender.Send(new ListSessionsQuery("t1"));

        Assert.Empty(list.Value);
        Assert.Contains(await _sessions.ListAsync(default), s => s.Kind == CeremonyKind.Retrospective);
    }

    [Fact]
    public async Task Approve_FromPortal_WritesEveryChangeWithPortalAttributionOnlyAfterApproval()
    {
        await StartAsync();
        var id = (await Sender.Send(new CreateCeremonyPackCommand("t1", CeremonyKind.SprintReview))).Value;
        Assert.Empty(_writer.Written);

        await Sender.Send(new ApproveFollowUpCommand(id, "Sam", ApprovalChannel.Portal));

        Assert.Single(_writer.Written);
        Assert.All(_writer.Written, w => Assert.Equal("Approved by Sam in the portal, proposed by MCM Scrum Agent from the 2026-01-08 sprint review.", w.Attribution));
        Assert.Equal(FollowUpStatus.Applied, (await _followUps.GetAsync(id, default))!.Status);
    }

    [Fact]
    public async Task Reject_Review_WritesNothing()
    {
        await StartAsync();
        var id = (await Sender.Send(new CreateCeremonyPackCommand("t1", CeremonyKind.SprintReview))).Value;

        await Sender.Send(new RejectFollowUpCommand(id, "Sam"));

        Assert.Empty(_writer.Written);
    }

    [Fact]
    public async Task Create_ModelSummaryWithInventedNumber_FallsBackToTheComputedSummary()
    {
        await StartAsync("{\"summary\":\"We delivered 99 of 16 points.\"}");

        var id = (await Sender.Send(new CreateCeremonyPackCommand("t1", CeremonyKind.SprintReview))).Value;

        Assert.StartsWith("8 of 16 points were delivered", (await _followUps.GetAsync(id, default))!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_ModelSummaryUsingOnlyPackNumbers_IsUsed()
    {
        await StartAsync("{\"summary\":\"The team delivered 8 of 16 points; nothing is written until you approve.\"}");

        var id = (await Sender.Send(new CreateCeremonyPackCommand("t1", CeremonyKind.SprintReview))).Value;

        Assert.StartsWith("The team delivered 8 of 16 points", (await _followUps.GetAsync(id, default))!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_DailyScrum_IsInvalid()
    {
        await StartAsync();

        var result = await Sender.Send(new CreateCeremonyPackCommand("t1", CeremonyKind.DailyScrum));

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }

    public ValueTask DisposeAsync() => _sp.DisposeAsync();
}
