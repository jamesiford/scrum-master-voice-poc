using Mcm.Scrum.Domain.Voice;

namespace Mcm.Scrum.Domain.Tests.Voice;

public sealed class UtteranceClassifierTests
{
    private static readonly NameRef[] Roster = [new("m-daniel", "Daniel Park"), new("m-maya", "Maya Lin"), new("m-admin", "Admin")];

    private static Interpretation Say(string text, string speaker, string? holder = "m-admin") =>
        UtteranceClassifier.Classify(text, speaker, holder, Roster);

    [Theory]
    [InlineData("Admin had to run to the bathroom, he'll be back in a few minutes", "m-maya", UtteranceIntent.StepAway, "m-admin")]
    [InlineData("Sorry, can we circle back to me? I need a minute", "m-admin", UtteranceIntent.StepAway, "m-admin")]
    [InlineData("Daniel stepped out for a call", "m-maya", UtteranceIntent.StepAway, "m-daniel")]
    public void Classify_StepAway_FindsWhoIsAway(string text, string speaker, UtteranceIntent intent, string target)
    {
        var r = Say(text, speaker);

        Assert.Equal(intent, r.Intent);
        Assert.Equal(target, r.TargetMemberId);
    }

    [Theory]
    [InlineData("I'll give Admin's update, he asked me to", "m-maya", "m-admin", "m-maya")]
    [InlineData("I can do his update for him", "m-daniel", "m-admin", "m-daniel")]
    [InlineData("Maya will give my update", "m-admin", "m-admin", "m-maya")]
    public void Classify_Proxy_FindsWhoSpeaksForWhom(string text, string speaker, string target, string by)
    {
        var r = Say(text, speaker);

        Assert.Equal(UtteranceIntent.ProxyUpdate, r.Intent);
        Assert.Equal(target, r.TargetMemberId);
        Assert.Equal(by, r.ByMemberId);
    }

    [Theory]
    [InlineData("What am I supposed to report on?", "m-admin", UtteranceIntent.AskWhatToReport)]
    [InlineData("Which items do I need to update?", "m-admin", UtteranceIntent.AskWhatToReport)]
    [InlineData("Actually I meant story 5, not story 1", "m-admin", UtteranceIntent.Correction)]
    [InlineData("Is that one overdue?", "m-admin", UtteranceIntent.AskItemInfo)]
    [InlineData("Yesterday I finished the login work", "m-maya", UtteranceIntent.OutOfTurnUpdate)]
    [InlineData("Yeah that gateway thing was painful", "m-maya", UtteranceIntent.Chatter)]
    [InlineData("I'm back, sorry", "m-maya", UtteranceIntent.BackNow)]
    [InlineData("I finished the refund tests and I'm on the retry story", "m-admin", UtteranceIntent.Update)]
    public void Classify_Utterance_PicksTheIntent(string text, string speaker, UtteranceIntent expected) =>
        Assert.Equal(expected, Say(text, speaker).Intent);

    [Fact]
    public void Classify_BackInAFewWeeksStyleChatterFromOthers_IsNotAway() =>
        Assert.Equal(UtteranceIntent.Chatter, Say("That will be back in the sprint backlog soon", "m-maya").Intent);
}
