using System.Text.Json.Nodes;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Infrastructure.Teams;

namespace Mcm.Scrum.Infrastructure.Tests.Teams;

public sealed class CardRendererTests
{
    [Fact]
    public void Render_FollowUp_HasApproveRejectActionsAndFacts()
    {
        var id = Guid.NewGuid();
        var message = new TeamsMessage(CardKind.FollowUp, "t", Guid.NewGuid(), "k", "Follow-up for Priya", "Please confirm", [new CardFact("Work item 1", "On track")], id);

        var card = JsonNode.Parse(CardRenderer.Render(message))!;

        Assert.Equal("Follow-up for Priya", card["body"]![0]!["text"]!.GetValue<string>());
        Assert.Equal(["approve", "reject"], card["actions"]!.AsArray().Select(a => a!["verb"]!.GetValue<string>()).ToArray());
        Assert.Equal(id.ToString(), card["actions"]![0]!["data"]!["followUpId"]!.GetValue<string>());
        Assert.Equal("On track", card["body"]![2]!["facts"]![0]!["value"]!.GetValue<string>());
    }
}
