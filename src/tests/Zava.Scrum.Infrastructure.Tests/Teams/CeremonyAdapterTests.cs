using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Infrastructure.Ado;
using Zava.Scrum.Infrastructure.Teams;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure.Tests.Teams;

public sealed class CeremonyAdapterTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 8, 10, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(CardKind.RetroActions)]
    [InlineData(CardKind.ReviewPack)]
    public void Render_CeremonyPack_HasApproveAllAndReject(CardKind kind)
    {
        var message = new TeamsMessage(kind, "t", Guid.NewGuid(), "k", "Pack", "Summary", [new CardFact("Carry-over", "2 items")], Guid.NewGuid());

        var card = JsonNode.Parse(CardRenderer.Render(message))!;

        Assert.Equal(["Approve all", "Reject"], card["actions"]!.AsArray().Select(a => a!["title"]!.GetValue<string>()).ToArray());
        Assert.Equal(["approve", "reject"], card["actions"]!.AsArray().Select(a => a!["verb"]!.GetValue<string>()).ToArray());
    }

    [Fact]
    public void Render_Unblock_HasNoActions()
    {
        var card = JsonNode.Parse(CardRenderer.Render(new TeamsMessage(CardKind.Unblock, "t", Guid.NewGuid(), "k", "Help", "Can you help?", [])))!;

        Assert.Null(card["actions"]);
    }

    [Fact]
    public void Select_Recap_GoesToTheRecapChannelAndMeetingChatOnly()
    {
        var message = new TeamsMessage(CardKind.Recap, "t", Guid.NewGuid(), "k", "r", "r", [], Route: CardRoute.Recap, RecapChannelKey: "19:chan@thread.tacv2");

        var keys = CardRouting.Select(message, ["a-dm", "19:chan@thread.tacv2", "19:meeting_abc@thread.v2", "b-dm"]);

        Assert.Equal(["19:chan@thread.tacv2", "19:meeting_abc@thread.v2"], keys);
    }

    [Fact]
    public void Select_RecapWithNeitherChannelNorMeetingChat_FallsBackToEveryConversation() =>
        Assert.Equal(["a-dm", "b-dm"], CardRouting.Select(new TeamsMessage(CardKind.Recap, "t", Guid.NewGuid(), "k", "r", "r", [], Route: CardRoute.Recap), ["a-dm", "b-dm"]));

    [Fact]
    public void Select_FollowUpCard_GoesToEveryConversation() =>
        Assert.Equal(["a-dm", "19:chan"], CardRouting.Select(new TeamsMessage(CardKind.FollowUp, "t", Guid.NewGuid(), "k", "r", "r", [], RecapChannelKey: "19:chan"), ["a-dm", "19:chan"]));

    [Fact]
    public void ToRequest_Email_IsAGraphSendMailBody()
    {
        var json = JsonSerializer.Serialize(GraphEmailSender.ToRequest(new EmailMessage("marcus@x.com", "Subject", "Body", null)));

        var doc = JsonNode.Parse(json)!;

        Assert.Equal("marcus@x.com", doc["message"]!["toRecipients"]![0]!["emailAddress"]!["address"]!.GetValue<string>());
        Assert.Equal("Subject", doc["message"]!["subject"]!.GetValue<string>());
        Assert.Equal("Text", doc["message"]!["body"]!["contentType"]!.GetValue<string>());
    }

    [Fact]
    public async Task Writer_SetIteration_PatchesIterationPathWithPortalAttribution()
    {
        var spy = new SpyHandler();

        await Apply(spy, ProposedChange.SetIteration(7, "P\\Sprint 2", "carry-over"), CeremonyKind.SprintPlanning);

        Assert.Equal(HttpMethod.Patch, spy.Method);
        Assert.Contains("_apis/wit/workitems/7", spy.Url, StringComparison.Ordinal);
        Assert.Equal(["System.IterationPath", "System.History"], spy.Paths());
        Assert.Contains("Approved by Dana in the portal, proposed by MCM Scrum Agent from the 2026-01-08 sprint planning.", spy.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Writer_Assign_PatchesAssignedTo()
    {
        var spy = new SpyHandler();

        await Apply(spy, ProposedChange.Assign(7, "dana@x.com", "fewest points"), CeremonyKind.SprintPlanning);

        Assert.Equal(["System.AssignedTo", "System.History"], spy.Paths());
        Assert.Contains("dana@x.com", spy.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Writer_CreateWorkItem_PostsToTheProjectOfItsIterationWithAttributionAsFirstComment()
    {
        var spy = new SpyHandler();
        var item = new NewWorkItem("Task", "Retro action: fix retries", "details", ["retro-action"], "MyProject\\Sprint 2", "dana@x.com");

        await Apply(spy, ProposedChange.Create(item, "retro"), CeremonyKind.Retrospective);

        Assert.Equal(HttpMethod.Post, spy.Method);
        Assert.StartsWith("MyProject/_apis/wit/workitems/$Task", spy.Url, StringComparison.Ordinal);
        Assert.Equal(["System.Title", "System.Description", "System.IterationPath", "System.History", "System.Tags", "System.AssignedTo"], spy.Paths());
        Assert.Contains("from the retrospective.", spy.Body, StringComparison.Ordinal);
    }

    private static async Task Apply(SpyHandler spy, ProposedChange change, CeremonyKind ceremony)
    {
        using var http = new HttpClient(spy) { BaseAddress = new Uri("https://dev.azure.com/org/") };
        var f = FollowUp.Create(Guid.NewGuid(), "t1", Guid.NewGuid(), null, FollowUpKind.PlanningPack, "m", [change], new DateOnly(2026, 1, 8), Now, ceremony, forScrumMaster: true);
        f.Approve("Dana", Now, ApprovalChannel.Portal);

        var result = await new AdoWorkItemWriter(http, Options.Create(new DemoOptions { HonorOwnerTags = false }), NullLogger<AdoWorkItemWriter>.Instance).ApplyAsync(f.GetApprovedChanges().Value[0], CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    private sealed class SpyHandler : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }

        public string Url { get; private set; } = string.Empty;

        public string Body { get; private set; } = string.Empty;

        public IEnumerable<string> Paths() =>
            JsonNode.Parse(Body)!.AsArray().Select(o => o!["path"]!.GetValue<string>().Replace("/fields/", string.Empty, StringComparison.Ordinal));

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Url = request.RequestUri!.PathAndQuery.TrimStart('/');
            Url = Url.StartsWith("org/", StringComparison.Ordinal) ? Url["org/".Length..] : Url;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
