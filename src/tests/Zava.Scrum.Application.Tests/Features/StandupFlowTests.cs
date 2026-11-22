using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Standups;
using Zava.Scrum.Application.Features.Standups.CompleteSpeakerTurn;
using Zava.Scrum.Application.Features.Standups.GetNextSpeaker;
using Zava.Scrum.Application.Features.Standups.GetTimeRemaining;
using Zava.Scrum.Application.Features.Standups.GetTurnInstruction;
using Zava.Scrum.Application.Features.Standups.MarkAbsent;
using Zava.Scrum.Application.Features.Standups.ParkTopic;
using Zava.Scrum.Application.Features.Standups.ReconcileUpdate;
using Zava.Scrum.Application.Features.Standups.RecordStandupEvent;
using Zava.Scrum.Application.Features.Standups.StartStandup;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Application.Tools;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Voice;

namespace Zava.Scrum.Application.Tests.Features;

public sealed class StandupFlowTests
{
    [Fact]
    public async Task Start_PreparedSession_GoesLiveWithTimebox_ThenSecondStartConflicts()
    {
        await using var h = await StandupHarness.CreateAsync();

        var first = await h.Sender.Send(new StartStandupCommand(h.SessionId, "simulated", null, 7));
        var second = await h.Sender.Send(new StartStandupCommand(h.SessionId, "simulated"));

        var s = await h.LoadAsync();
        Assert.True(first.IsSuccess);
        Assert.Equal(ErrorType.Conflict, second.Error?.Type);
        Assert.Equal((StandupStatus.Live, 7), (s.Status, s.StandupMinutes));
    }

    [Fact]
    public async Task Start_AcsWithoutJoinUrl_IsInvalid()
    {
        await using var h = await StandupHarness.CreateAsync();

        var result = await h.Sender.Send(new StartStandupCommand(h.SessionId, "acs"));

        Assert.Equal(ErrorType.Validation, result.Error?.Type);
    }

    [Fact]
    public async Task GetNextSpeaker_WalksSpeakingOrderIncludingAbsentThenFinishes()
    {
        await using var h = await StandupHarness.CreateAsync();
        await h.Sender.Send(new MarkAbsentCommand(h.SessionId, "m-marcus"));

        var first = (await h.Sender.Send(new GetNextSpeakerQuery(h.SessionId))).Value;
        await h.Sender.Send(new CompleteSpeakerTurnCommand(h.SessionId, "m-priya"));
        var second = (await h.Sender.Send(new GetNextSpeakerQuery(h.SessionId))).Value;
        await h.Sender.Send(new CompleteSpeakerTurnCommand(h.SessionId, "m-marcus"));
        var done = (await h.Sender.Send(new GetNextSpeakerQuery(h.SessionId))).Value;

        Assert.Equal(("m-priya", "Priya", false), (first.MemberId, first.FirstName, first.Absent));
        Assert.Equal(("m-marcus", true), (second.MemberId, second.Absent));
        Assert.Null(done.MemberId);
    }

    [Fact]
    public async Task MarkAbsent_UnknownMember_IsNotFound()
    {
        await using var h = await StandupHarness.CreateAsync();

        var result = await h.Sender.Send(new MarkAbsentCommand(h.SessionId, "m-nobody"));

        Assert.Equal("session.member_not_in_brief", result.Error?.Code);
    }

    [Fact]
    public async Task RecordEvent_DropsWorkItemIdsOutsideTheSprint()
    {
        await using var h = await StandupHarness.CreateAsync();

        await h.Sender.Send(new RecordStandupEventCommand(h.SessionId, StandupEventType.Blocker, "m-marcus", "Blocked on database access", [12, 9999]));

        var evt = Assert.Single((await h.LoadAsync()).Events);
        Assert.Equal([12], evt.WorkItemIds);
    }

    [Fact]
    public async Task ParkTopic_RecordsParkingLotEvent()
    {
        await using var h = await StandupHarness.CreateAsync();

        await h.Sender.Send(new ParkTopicCommand(h.SessionId, "Gateway upgrade"));

        Assert.Equal("Gateway upgrade", Assert.Single((await h.LoadAsync()).Events, e => e.Type == StandupEventType.ParkingLot).Text);
    }

    [Fact]
    public async Task GetTimeRemaining_BeforeStart_ReturnsFullTimebox()
    {
        await using var h = await StandupHarness.CreateAsync();

        var t = (await h.Sender.Send(new GetTimeRemainingQuery(h.SessionId))).Value;

        Assert.Equal((900, false), (t.RemainingSeconds, t.Warn));
    }

    [Fact]
    public async Task Reconcile_ModelMentionsUnknownIds_DropsThemAndFallsBackToDeterministicQuestion()
    {
        var suggestion = new ReconcileSuggestion(
            true,
            [1, 4242],
            ["story 4242 is at risk"],
            "Is story 4242 done?",
            [new SuggestedEvent(StandupEventType.Commitment, "Finish story 4242", [4242]), new SuggestedEvent(StandupEventType.Commitment, "Finish the retry story", [1, 4242])]);
        await using var h = await StandupHarness.CreateAsync(suggestion);

        var r = (await h.Sender.Send(new ReconcileUpdateQuery(h.SessionId, "m-priya", "still on story 1"))).Value;

        Assert.Equal([1], r.MentionedWorkItemIds);
        Assert.Empty(r.Risks);
        Assert.Null(r.SuggestedQuestion);
        var kept = Assert.Single(r.Events);
        Assert.Equal([1], kept.WorkItemIds);
    }

    [Fact]
    public async Task Reconcile_MentionedFlaggedItem_IsCoveredByCodeAndNotAskedAgain()
    {
        var suggestion = new ReconcileSuggestion(true, [12], [], "Story 1 is stale. Status?", []);
        await using var h = await StandupHarness.CreateAsync(suggestion);

        var r = (await h.Sender.Send(new ReconcileUpdateQuery(h.SessionId, "m-priya", "working on story 1"))).Value;

        Assert.Equal([1], r.MentionedWorkItemIds);
        Assert.Null(r.SuggestedQuestion);
    }

    [Fact]
    public async Task Reconcile_AnswerWithoutNamingAnItem_IsAttributedToTheAskedItem()
    {
        await using var h = await StandupHarness.CreateAsync(null);

        var r = (await h.Sender.Send(new ReconcileUpdateQuery(h.SessionId, "m-priya", "yes, it is on track", "Story 1 is stale. Still on track?", FocusItemId: 1))).Value;

        Assert.Equal([1], r.MentionedWorkItemIds);
    }

    [Fact]
    public async Task Reconcile_NoModel_UsesFlagBasedQuestionOnlyWhenSomethingIsFlagged()
    {
        await using var h = await StandupHarness.CreateAsync(null);

        var r = (await h.Sender.Send(new ReconcileUpdateQuery(h.SessionId, "m-marcus", "nothing much to report"))).Value;

        Assert.Contains("12", r.SuggestedQuestion, StringComparison.Ordinal);
        Assert.Equal(12, r.QuestionItemId);
    }

    [Theory]
    [InlineData(SpeakReason.Open, "turn-open", "Open {teamName} {sprintGoal} {daysLeft}", "Open Payments Ship retries 6")]
    [InlineData(SpeakReason.CallOn, "turn-call-on", "Invite {firstName}", "Invite Priya")]
    public async Task TurnInstruction_RendersTemplateWithSessionFacts(SpeakReason reason, string name, string template, string expected)
    {
        await using var h = await StandupHarness.CreateAsync(prompts: new Dictionary<string, string> { [name] = template });

        var text = (await h.Sender.Send(new GetTurnInstructionQuery(h.SessionId, reason, "m-priya"))).Value;

        Assert.Equal(expected, text);
    }

    [Fact]
    public async Task TurnInstruction_MissingPrompt_IsNotFoundNotAnException()
    {
        await using var h = await StandupHarness.CreateAsync();

        var result = await h.Sender.Send(new GetTurnInstructionQuery(h.SessionId, SpeakReason.Recap));

        Assert.Equal("prompt.not_found", result.Error?.Code);
    }

    [Fact]
    public async Task TurnInstruction_Recap_ListsBlockersParkedAndAbsent()
    {
        await using var h = await StandupHarness.CreateAsync(prompts: new Dictionary<string, string> { ["turn-recap"] = "B={blockers}|P={parked}|A={absent}" });
        await h.Sender.Send(new RecordStandupEventCommand(h.SessionId, StandupEventType.Blocker, "m-marcus", "Needs database access"));
        await h.Sender.Send(new ParkTopicCommand(h.SessionId, "Gateway upgrade"));
        await h.Sender.Send(new MarkAbsentCommand(h.SessionId, "m-priya"));

        var text = (await h.Sender.Send(new GetTurnInstructionQuery(h.SessionId, SpeakReason.Recap))).Value;

        Assert.Equal("B=Needs database access|P=Gateway upgrade|A=Priya", text);
    }

    [Fact]
    public async Task Tools_ReconcileUpdate_ReturnsGroundedJsonAndUnknownToolIsAnErrorObject()
    {
        await using var h = await StandupHarness.CreateAsync(new ReconcileSuggestion(true, [1], [], "Story 1 is stale. Status?", []));
        var tools = h.Get<IScrumTools>();

        var ok = await tools.InvokeAsync(h.SessionId, "reconcile_update", """{"memberId":"m-priya","utterance":"story 1"}""", CancellationToken.None);
        var bad = await tools.InvokeAsync(h.SessionId, "delete_everything", "{}", CancellationToken.None);

        Assert.Contains("\"mentionedWorkItemIds\":[1]", ok, StringComparison.Ordinal);
        Assert.Contains("error", bad, StringComparison.Ordinal);
        Assert.Contains(tools.Definitions, d => d.Name == "reconcile_update");
    }

    [Fact]
    public async Task Reconcile_BlockerEventThatSaysThereAreNone_IsDropped()
    {
        var suggestion = new ReconcileSuggestion(
            true,
            [],
            [],
            null,
            [
                new SuggestedEvent(StandupEventType.Blocker, "No blockers right now.", []),
                new SuggestedEvent(StandupEventType.Blocker, "Nothing is blocking the work.", []),
                new SuggestedEvent(StandupEventType.Blocker, "Blocked on bug 12 by the sandbox gateway.", [12]),
            ]);
        await using var h = await StandupHarness.CreateAsync(suggestion);

        var r = (await h.Sender.Send(new ReconcileUpdateQuery(h.SessionId, "m-marcus", "no blockers"))).Value;

        Assert.Equal("Blocked on bug 12 by the sandbox gateway.", Assert.Single(r.Events).Text);
    }

    [Fact]
    public async Task GetNextSpeaker_ReportsWhetherMemberHasFlaggedItems()
    {
        await using var h = await StandupHarness.CreateAsync();

        var next = (await h.Sender.Send(new GetNextSpeakerQuery(h.SessionId))).Value;

        Assert.True(next.HasFlaggedItems);
    }

    [Fact]
    public void PromptTemplate_UnknownPlaceholder_IsLeftVisible()
    {
        Assert.Equal("Hi Ann {typo}", PromptTemplate.Render("Hi {name} {typo}", new Dictionary<string, string> { ["name"] = "Ann" }));
    }
}
