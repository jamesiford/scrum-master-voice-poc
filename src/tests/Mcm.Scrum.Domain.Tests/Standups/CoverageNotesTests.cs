using Mcm.Scrum.Domain.Flags;
using Mcm.Scrum.Domain.Standups;
using Mcm.Scrum.Domain.Voice;
using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Domain.Tests.Standups;

public sealed class CoverageNotesTests
{
    [Theory]
    [InlineData("Is that one overdue?", true)]
    [InlineData("What was the due date?", true)]
    [InlineData("I will finish it tomorrow", false)]
    [InlineData("It is overdue", false)]
    public void IsItemQuestion_Utterance_DetectsQuestions(string text, bool expected) =>
        Assert.Equal(expected, ItemQuestionDetector.IsItemQuestion(text));

    [Fact]
    public void UncoveredFlagged_SkipsCoveredAndUnflagged_MostUrgentFirst()
    {
        BriefItem Item(int id, params Flag[] flags) => new(new WorkItemId(id), "t", "Bug", "Active", 1, flags);
        var brief = new PreBrief("t", "p", "g", 3, new(10, 4, 6, 3, false), [new MemberBrief("m", "M", [Item(1, Flag.Stale), Item(2, Flag.Blocked), Item(3), Item(4, Flag.Overdue)], "q")], [], [], "s");

        var result = CoverageNotes.UncoveredFlagged(brief, new HashSet<int> { 4 });

        Assert.Equal([2, 1], result.Select(i => i.Id.Value));
        Assert.Equal("bug 2 (blocked), bug 1 (stale)", CoverageNotes.Describe(result));
    }

    [Fact]
    public void Noted_BlockersAndCommitmentsOnly() =>
        Assert.Equal("blocker: vendor delay", CoverageNotes.Noted([(StandupEventType.Blocker, "vendor delay"), (StandupEventType.Decision, "x")]));
}
