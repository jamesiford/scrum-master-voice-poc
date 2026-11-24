using Zava.Scrum.Domain.Flags;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Tests.Standups;

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

    [Fact]
    public void Detect_AdjacentTitleWords_LinkTheItemWhenOneWordIsUnique()
    {
        BriefItem[] maya =
        [
            Item(30, "User Story", "Settlement file validation"),
            Item(31, "User Story", "Update PCI scan exceptions"),
            Item(32, "Task", "Review settlement schema change"),
        ];

        HashSet<int> known = [30, 31, 32];
        Assert.Equal([32], ItemMentions.Detect("Yesterday I reviewed the settlement schema", maya, known));
        Assert.Equal([30], ItemMentions.Detect("Today I'm continuing the settlement file validation", maya, known));
        Assert.Empty(ItemMentions.Detect("I need the schema from them", maya, known));
    }
}

