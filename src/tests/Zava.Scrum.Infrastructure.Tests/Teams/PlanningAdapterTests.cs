using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Planning;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Infrastructure.Ado;
using Zava.Scrum.Infrastructure.Persistence;
using Zava.Scrum.Infrastructure.Teams;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure.Tests.Teams;

public sealed class PlanningAdapterTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 1, 8, 10, 0, 0, TimeSpan.Zero);
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "planning-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Render_PlanningCard_HasApprovePlanAndRejectActionsCarryingThePackId()
    {
        var id = Guid.NewGuid();
        var card = JsonNode.Parse(CardRenderer.Render(new TeamsMessage(CardKind.PlanningPack, "t", id, $"planning:{id}", "Sprint planning", "Summary", [new CardFact("Plan #10", "Item")], id)))!;

        var actions = card["actions"]!.AsArray();

        Assert.Equal(["Approve plan", "Reject"], actions.Select(a => a!["title"]!.GetValue<string>()));
        Assert.Equal(["approve-plan", "reject-plan"], actions.Select(a => a!["verb"]!.GetValue<string>()));
        Assert.All(actions, a => Assert.Equal(id.ToString(), a!["data"]!["packId"]!.GetValue<string>()));
    }

    [Fact]
    public async Task Writer_AssignInDemoMode_ReplacesTheOwnerTagInsteadOfSettingAnIdentityTheSandboxDoesNotHave()
    {
        var spy = new TagHandler("bug; demo-owner-maya; Blocked");

        await Apply(spy, ProposedChange.Assign(7, "Daniel Park", "least loaded", "daniel"), honorOwnerTags: true);

        var patch = spy.Requests.Single(r => r.Method == HttpMethod.Patch);
        var ops = JsonNode.Parse(patch.Body)!.AsArray();
        Assert.Equal(["System.Tags", "System.History"], ops.Select(o => o!["path"]!.GetValue<string>().Replace("/fields/", string.Empty, StringComparison.Ordinal)));
        Assert.Equal("bug; Blocked; demo-owner-daniel", ops[0]!["value"]!.GetValue<string>());
        Assert.DoesNotContain("AssignedTo", patch.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Writer_AssignOutsideDemoMode_SetsAssignedTo()
    {
        var spy = new TagHandler(string.Empty);

        await Apply(spy, ProposedChange.Assign(7, "dana@x.com", "least loaded", "dana"), honorOwnerTags: false);

        Assert.Contains("System.AssignedTo", spy.Requests.Single(r => r.Method == HttpMethod.Patch).Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Repository_PackRoundTripsThroughTheStoreAndLatestWins()
    {
        var repo = new JsonFilePlanningPackRepository(new FileCollectionStore(Options.Create(new StoreOptions { DataPath = _dir })));
        var proposal = new PlanningProposal(
            new VelocityRange(32, 40),
            [new MemberCapacity("m1", "Daniel", 100, 10.7, 13.3, 5)],
            [new PlannedItem(10, "User Story", "T", 5, PlanningSource.Backlog, "m1", "Daniel", true)],
            [new ExcludedItem(11, "U", null, ["No story points"])],
            5,
            CommitmentLevel.Under,
            ["Under-committed"]);
        var older = PlanningPack.Prepare(Guid.NewGuid(), "t1", Now.AddDays(-1), "P\\S1", "P\\S2", true, proposal, "older", false);
        var newer = PlanningPack.Prepare(Guid.NewGuid(), "t1", Now, "P\\S1", "P\\S2", true, proposal, "newer", true);
        await repo.SaveAsync(older, CancellationToken.None);
        await repo.SaveAsync(newer, CancellationToken.None);
        newer.Reject("Dana", Now);
        await repo.SaveAsync(newer, CancellationToken.None);

        var latest = (await repo.GetLatestAsync("t1", CancellationToken.None))!;

        Assert.Equal((newer.Id, PlanningPackStatus.Rejected, "Dana"), (latest.Id, latest.Data.Status, latest.Data.DecidedBy));
        Assert.Equal(proposal.Excluded[0].Reasons, latest.Data.Proposal.Excluded[0].Reasons);
        Assert.Equal(PlanningSource.Backlog, latest.Data.Proposal.Selected[0].Source);
        Assert.Equal(older.Id, (await repo.GetAsync(older.Id, CancellationToken.None))!.Id);
        Assert.Null(await repo.GetLatestAsync("other", CancellationToken.None));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    private static async Task Apply(TagHandler spy, ProposedChange change, bool honorOwnerTags)
    {
        using var http = new HttpClient(spy) { BaseAddress = new Uri("https://dev.azure.com/org/") };
        var f = FollowUp.Create(Guid.NewGuid(), "t1", Guid.NewGuid(), null, FollowUpKind.PlanningPack, "m", [change], new DateOnly(2026, 1, 8), Now, CeremonyKind.SprintPlanning, forScrumMaster: true);
        f.Approve("Dana", Now, ApprovalChannel.Teams);

        var result = await new AdoWorkItemWriter(http, Options.Create(new DemoOptions { HonorOwnerTags = honorOwnerTags }), NullLogger<AdoWorkItemWriter>.Instance)
            .ApplyAsync(f.GetApprovedChanges().Value[0], CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    private sealed class TagHandler(string tags) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.Method, request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));
            var body = request.Method == HttpMethod.Get ? $$"""{ "fields": { "System.Tags": "{{tags}}" } }""" : "{}";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
