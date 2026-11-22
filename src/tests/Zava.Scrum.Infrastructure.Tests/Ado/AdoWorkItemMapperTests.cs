using System.Text.Json;
using Zava.Scrum.Infrastructure.Ado;

namespace Zava.Scrum.Infrastructure.Tests.Ado;

public sealed class AdoWorkItemMapperTests
{
    private static JsonElement Item(string fieldsJson) => JsonDocument.Parse($$"""{ "id": 4521, "fields": {{{fieldsJson}}} }""").RootElement;

    private const string Base = """
        "System.Title": "Payment retry", "System.WorkItemType": "User Story", "System.State": "Active",
        "Microsoft.VSTS.Common.StateChangeDate": "2026-01-08T10:00:00Z", "System.ChangedDate": "2026-01-08T10:00:00Z",
        "Microsoft.VSTS.Scheduling.StoryPoints": 5.0
        """;

    [Fact]
    public void Map_BasicFields_AreCopied()
    {
        var item = AdoWorkItemMapper.Map(Item(Base + """, "System.AssignedTo": { "displayName": "A B", "uniqueName": "a@x.com" }"""), new DemoOptions());

        Assert.Equal((4521, "Payment retry", "User Story", "Active", "a@x.com", 5d), (item.Id.Value, item.Title, item.Type, item.State, item.AssignedTo, item.StoryPoints));
    }

    [Fact]
    public void Map_AgeTagWithHonorOn_ShiftsStateChangeBackThatManyDays()
    {
        var item = AdoWorkItemMapper.Map(Item(Base + """, "System.Tags": "zava-demo; demo-age-3" """), new DemoOptions { HonorAgeTags = true });

        Assert.Equal(new DateTimeOffset(2026, 1, 5, 10, 0, 0, TimeSpan.Zero), item.StateChangedAt);
        Assert.Equal(new DateTimeOffset(2026, 1, 8, 10, 0, 0, TimeSpan.Zero), item.ChangedAt);
    }

    [Fact]
    public void Map_AgeTagWithHonorOff_LeavesDatesAlone()
    {
        var item = AdoWorkItemMapper.Map(Item(Base + """, "System.Tags": "demo-age-3" """), new DemoOptions { HonorAgeTags = false });

        Assert.Equal(new DateTimeOffset(2026, 1, 8, 10, 0, 0, TimeSpan.Zero), item.StateChangedAt);
    }

    [Fact]
    public void Map_QuietTag_ShiftsChangedDateOnly()
    {
        var item = AdoWorkItemMapper.Map(Item(Base + """, "System.Tags": "demo-quiet-2" """), new DemoOptions());

        Assert.Equal(new DateTimeOffset(2026, 1, 6, 10, 0, 0, TimeSpan.Zero), item.ChangedAt);
        Assert.Equal(new DateTimeOffset(2026, 1, 8, 10, 0, 0, TimeSpan.Zero), item.StateChangedAt);
    }

    [Fact]
    public void Map_OwnerTag_SetsOwnerKeyAndBlockedTagIsKept()
    {
        var item = AdoWorkItemMapper.Map(Item(Base + """, "System.Tags": "Blocked; demo-owner-priya" """), new DemoOptions());

        Assert.Equal("priya", item.OwnerKey);
        Assert.True(item.HasTag("Blocked"));
    }

    [Fact]
    public void Map_NoAssigneeNoOwner_IsUnassigned()
    {
        var item = AdoWorkItemMapper.Map(Item(Base), new DemoOptions());

        Assert.Null(item.AssignedTo);
        Assert.Null(item.OwnerKey);
    }

    [Fact]
    public void Map_FinishDate_BecomesDueDate()
    {
        var item = AdoWorkItemMapper.Map(Item(Base + """, "Microsoft.VSTS.Scheduling.FinishDate": "2026-01-06T00:00:00Z" """), new DemoOptions());

        Assert.Equal(new DateTimeOffset(2026, 1, 6, 0, 0, 0, TimeSpan.Zero), item.DueDate);
    }
}
