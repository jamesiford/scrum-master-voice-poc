using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Tests.Standups;

public sealed class WorkItemIdGuardTests
{
    private static readonly HashSet<WorkItemId> Known = [new(4521), new(4602)];

    [Theory]
    [InlineData("Story 4521 has been Active for three days.", true)]
    [InlineData("Bug #4602 is blocked.", true)]
    [InlineData("Story 4521 and bug 4602 are linked.", true)]
    [InlineData("Nothing flagged, 3 days left.", true)]
    [InlineData("Story 9999 is stale.", false)]
    [InlineData("Story 4521 depends on #1234.", false)]
    public void IsGrounded_Text_RejectsUnknownIds(string text, bool expected)
    {
        Assert.Equal(expected, WorkItemIdGuard.IsGrounded(text, Known));
    }
}
