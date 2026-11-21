using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Domain.Tests;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 1, 8, 10, 0, 0, TimeSpan.Zero);

    public static WorkItem Item(
        int id = 1,
        string type = "User Story",
        string state = "Active",
        string? assignedTo = "a@x.com",
        string? ownerKey = null,
        double daysInState = 0,
        double daysSinceChange = 0,
        double? points = null,
        DateTimeOffset? due = null,
        params string[] tags) =>
        new(new WorkItemId(id), $"Item {id}", type, state, assignedTo, ownerKey, Now.AddDays(-daysInState), Now.AddDays(-daysSinceChange), points, null, due, tags);
}
