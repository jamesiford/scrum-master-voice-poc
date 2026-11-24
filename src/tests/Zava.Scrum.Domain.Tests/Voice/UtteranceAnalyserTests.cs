using Zava.Scrum.Domain.Flags;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Voice;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Tests.Voice;

public sealed class UtteranceAnalyserTests
{
    private static readonly NameRef[] Roster = [new("m-daniel", "Daniel Park"), new("m-maya", "Maya Lin"), new("m-admin", "Admin")];

    private static readonly BriefItem[] Own =
    [
        new(new WorkItemId(1), "Payment retry with exponential backoff", "User Story", "Active", 1, [Flag.Stale]),
        new(new WorkItemId(14), "Write retry load test", "Task", "Active", 1, []),
    ];

    private static readonly HashSet<int> Known = [1, 5, 14];

    private static UtteranceReading Reading(
        UtteranceIntent? intent,
        double confidence = 0.9,
        string? target = null,
        string? by = null,
        int[]? ids = null,
        params ItemComment[] comments) =>
        new(intent, target, by, ids ?? [], comments, confidence);

    private static UtteranceAnalysis Analyse(string text, string speaker, UtteranceReading? reading, string? holder = "m-admin") =>
        UtteranceAnalyser.Analyse(text, speaker, holder, Roster, Own, Known, reading);

    [Fact]
    public void Analyse_NoReading_UsesRules()
    {
        var a = Analyse("I'm on story 1", "m-admin", null);

        Assert.Equal(AnalysisSource.Rules, a.Source);
        Assert.Equal(UtteranceIntent.Update, a.Interpretation.Intent);
        Assert.Equal([1], a.WorkItemIds);
        Assert.Empty(a.Comments);
    }

    [Fact]
    public void Analyse_LowConfidence_UsesRules()
    {
        var a = Analyse("I'm on story 1", "m-admin", Reading(UtteranceIntent.Chatter, confidence: 0.3));

        Assert.Equal(AnalysisSource.Rules, a.Source);
        Assert.Equal(UtteranceIntent.Update, a.Interpretation.Intent);
    }

    [Fact]
    public void Analyse_ModelReadsAQuestionTheRulesMiss_TrustsTheModel()
    {
        var a = Analyse("any idea how old that one is?", "m-maya", Reading(UtteranceIntent.AskItemInfo, ids: [14]));

        Assert.Equal(AnalysisSource.Model, a.Source);
        Assert.Equal(UtteranceIntent.AskItemInfo, a.Interpretation.Intent);
        Assert.Equal([14], a.WorkItemIds);
    }

    [Fact]
    public void Analyse_ModelNamesAnUnknownMember_FallsBackToRules()
    {
        var a = Analyse("Daniel stepped out for a call", "m-maya", Reading(UtteranceIntent.StepAway, target: "m-nobody"));

        Assert.Equal(AnalysisSource.Rules, a.Source);
        Assert.Equal("m-daniel", a.Interpretation.TargetMemberId);
    }

    [Fact]
    public void Analyse_ModelStepAway_KeepsTheRosterMember()
    {
        var a = Analyse("he is out for a few", "m-maya", Reading(UtteranceIntent.StepAway, target: "m-admin"));

        Assert.Equal(AnalysisSource.Model, a.Source);
        Assert.Equal(UtteranceIntent.StepAway, a.Interpretation.Intent);
        Assert.Equal("m-admin", a.Interpretation.TargetMemberId);
    }

    [Fact]
    public void Analyse_ProxyWithoutByMember_CreditsTheSpeaker()
    {
        var a = Analyse("I'll cover Daniel", "m-maya", Reading(UtteranceIntent.ProxyUpdate, target: "m-daniel"));

        Assert.Equal(UtteranceIntent.ProxyUpdate, a.Interpretation.Intent);
        Assert.Equal("m-daniel", a.Interpretation.TargetMemberId);
        Assert.Equal("m-maya", a.Interpretation.ByMemberId);
    }

    [Fact]
    public void Analyse_ProxyForThemselves_FallsBackToRules()
    {
        var a = Analyse("I'll do my update", "m-maya", Reading(UtteranceIntent.ProxyUpdate, target: "m-maya", by: "m-maya"));

        Assert.Equal(AnalysisSource.Rules, a.Source);
    }

    [Theory]
    [InlineData("m-admin", UtteranceIntent.OutOfTurnUpdate, UtteranceIntent.Update)]
    [InlineData("m-maya", UtteranceIntent.Update, UtteranceIntent.OutOfTurnUpdate)]
    [InlineData("m-admin", UtteranceIntent.Chatter, UtteranceIntent.Update)]
    [InlineData("m-maya", UtteranceIntent.Chatter, UtteranceIntent.Chatter)]
    public void Analyse_Role_DecidesBetweenUpdateAndChimeIn(string speaker, UtteranceIntent said, UtteranceIntent expected)
    {
        var a = Analyse("whatever", speaker, Reading(said));

        Assert.Equal(expected, a.Interpretation.Intent);
    }

    [Fact]
    public void Analyse_HolderSaysBackNow_IsRejected()
    {
        var a = Analyse("I'm here", "m-admin", Reading(UtteranceIntent.BackNow));

        Assert.Equal(AnalysisSource.Rules, a.Source);
    }

    [Fact]
    public void Analyse_ModelInventsAWorkItem_DropsTheId()
    {
        var a = Analyse("working on story 9999", "m-admin", Reading(UtteranceIntent.Update, ids: [1, 9999]));

        Assert.Equal([1], a.WorkItemIds);
    }

    [Fact]
    public void Analyse_ModelFindsNoItem_UsesTheOnesTheRulesFound()
    {
        var a = Analyse("I'm on story 1", "m-admin", Reading(UtteranceIntent.Update));

        Assert.Equal(AnalysisSource.Model, a.Source);
        Assert.Equal([1], a.WorkItemIds);
    }

    [Fact]
    public void Analyse_Comments_AreGroundedInTheSprint()
    {
        var a = Analyse(
            "blocked on the vendor for story 5, so Friday",
            "m-admin",
            Reading(
                UtteranceIntent.Update,
                ids: [5],
                comments:
                [
                    new ItemComment(CommentKind.Blocker, [5, 9999], "Waiting on the vendor for story 5"),
                    new ItemComment(CommentKind.DateChange, [], "Finish moves to Friday", "Friday"),
                    new ItemComment(CommentKind.Commitment, [5], "Finish story 7777 tomorrow"),
                    new ItemComment(CommentKind.Progress, [5], "  "),
                ]));

        Assert.Equal(2, a.Comments.Count);
        Assert.Equal([5], a.Comments[0].WorkItemIds);
        Assert.Equal(CommentKind.DateChange, a.Comments[1].Kind);
        Assert.Equal([5], a.Comments[1].WorkItemIds);
        Assert.Equal("Friday", a.Comments[1].NewDate);
    }

    [Fact]
    public void Analyse_CommentWithManyItemsInTheUtterance_StaysUnlinked()
    {
        var a = Analyse(
            "story 1 is done and task 14 is late",
            "m-admin",
            Reading(UtteranceIntent.Update, ids: [1, 14], comments: [new ItemComment(CommentKind.Blocker, [], "Something is stuck")]));

        Assert.Empty(Assert.Single(a.Comments).WorkItemIds);
    }
}
