using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Standups.ReconcileUpdate;
using Zava.Scrum.Application.Features.Voice.AnalyseUtterance;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Voice;

namespace Zava.Scrum.Application.Tests.Features;

public sealed class UtteranceAnalysisFlowTests
{
    private static readonly NameRef[] Roster = [new("m-priya", "Priya"), new("m-marcus", "Marcus")];

    [Fact]
    public async Task Analyse_ModelReading_IsGroundedAndUsed()
    {
        var reading = new UtteranceReading(UtteranceIntent.Update, null, null, [1, 9999], [new ItemComment(CommentKind.Blocker, [1], "Waiting on the vendor for story 1")], 0.9);
        await using var h = await StandupHarness.CreateAsync(reading: reading);

        var a = (await h.Sender.Send(new AnalyseUtteranceQuery(h.SessionId, "m-priya", "m-priya", "we are stuck waiting on the vendor", Roster))).Value;

        Assert.Equal(AnalysisSource.Model, a.Source);
        Assert.Equal([1], a.WorkItemIds);
        Assert.Equal(CommentKind.Blocker, Assert.Single(a.Comments).Kind);
    }

    [Fact]
    public async Task Analyse_ModelUnavailable_UsesTheRules()
    {
        await using var h = await StandupHarness.CreateAsync();

        var a = (await h.Sender.Send(new AnalyseUtteranceQuery(h.SessionId, "m-priya", "m-priya", "working on story 1", Roster))).Value;

        Assert.Equal(AnalysisSource.Rules, a.Source);
        Assert.Equal(UtteranceIntent.Update, a.Interpretation.Intent);
        Assert.Equal([1], a.WorkItemIds);
    }

    [Fact]
    public async Task Analyse_UnknownSession_Fails()
    {
        await using var h = await StandupHarness.CreateAsync();

        var r = await h.Sender.Send(new AnalyseUtteranceQuery(Guid.NewGuid(), "m-priya", "m-priya", "hello", Roster));

        Assert.False(r.IsSuccess);
    }

    [Fact]
    public async Task Reconcile_WithComments_RecordsThoseKindsInsteadOfTheReconcileModelsEvents()
    {
        var suggestion = new ReconcileSuggestion(true, [1], [], null, [new SuggestedEvent(StandupEventType.Commitment, "Finish story 1", [1])]);
        await using var h = await StandupHarness.CreateAsync(suggestion);
        IReadOnlyList<ItemComment> comments =
        [
            new(CommentKind.DateChange, [1], "Finish moves to Friday", "Friday"),
            new(CommentKind.Blocker, [1], "Waiting on the vendor"),
            new(CommentKind.Progress, [1], "Reviewed it yesterday"),
        ];

        var r = (await h.Sender.Send(new ReconcileUpdateQuery(h.SessionId, "m-priya", "story 1 slips to Friday, waiting on the vendor", Comments: comments))).Value;

        Assert.Equal([StandupEventType.DateChange, StandupEventType.Blocker], r.Events.Select(e => e.Type));
        Assert.All(r.Events, e => Assert.Equal([1], e.WorkItemIds));
    }

    [Fact]
    public async Task Reconcile_CommentsSayNoBlockers_RecordsNoBlocker()
    {
        await using var h = await StandupHarness.CreateAsync();

        var r = (await h.Sender.Send(new ReconcileUpdateQuery(h.SessionId, "m-priya", "no blockers", Comments: [new(CommentKind.Blocker, [], "No blockers today")]))).Value;

        Assert.Empty(r.Events);
    }

    [Fact]
    public async Task Reconcile_EmptyCommentsFromTheModel_AreAuthoritative()
    {
        var suggestion = new ReconcileSuggestion(true, [1], [], null, [new SuggestedEvent(StandupEventType.Commitment, "Finish story 1", [1])]);
        await using var h = await StandupHarness.CreateAsync(suggestion);

        var r = (await h.Sender.Send(new ReconcileUpdateQuery(h.SessionId, "m-priya", "yes", Comments: []))).Value;

        Assert.Empty(r.Events);
    }

    [Fact]
    public async Task Reconcile_AnalysedItemIds_AreMentionedAndInventedOnesDropped()
    {
        await using var h = await StandupHarness.CreateAsync();

        var r = (await h.Sender.Send(new ReconcileUpdateQuery(h.SessionId, "m-priya", "the backoff one is done", AnalysedItemIds: [1, 9999]))).Value;

        Assert.Equal([1], r.MentionedWorkItemIds);
    }
}
