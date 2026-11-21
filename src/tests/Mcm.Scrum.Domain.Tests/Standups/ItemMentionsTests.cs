using Mcm.Scrum.Domain.Flags;
using Mcm.Scrum.Domain.Standups;
using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Domain.Tests.Standups;

public sealed class ItemMentionsTests
{
    private static BriefItem Item(int id, string type, string title) => new(new WorkItemId(id), title, type, "Active", 1, [Flag.Stale]);

    private static readonly BriefItem[] Own =
    [
        Item(1, "User Story", "Payment retry with exponential backoff"),
        Item(14, "Task", "Write retry load test"),
    ];

    private static readonly HashSet<int> Known = [1, 14, 5];

    [Theory]
    [InlineData("I'm on story 1", new[] { 1 })]
    [InlineData("task fourteen is nearly done", new[] { 14 })]
    [InlineData("working on #14 today", new[] { 14 })]
    [InlineData("the exponential backoff part is done", new[] { 1 })]
    [InlineData("the load test needs a new date", new[] { 14 })]
    [InlineData("the retry work is going fine", new int[0])]
    [InlineData("I finished the refund API tests", new int[0])]
    [InlineData("story 99 is fine", new int[0])]
    [InlineData("story 5 is someone else's", new[] { 5 })]
    public void Detect_Utterance_FindsOnlyTheItemsMeant(string text, int[] expected) =>
        Assert.Equal(expected, ItemMentions.Detect(text, Own, Known));
}

