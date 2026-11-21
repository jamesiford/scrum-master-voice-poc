using System.Net;
using System.Text;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Flags;
using Mcm.Scrum.Domain.Kpis;
using Mcm.Scrum.Domain.Standups;
using Mcm.Scrum.Domain.Work;
using Mcm.Scrum.Infrastructure.Agents;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Mcm.Scrum.Infrastructure.Tests.Agents;

public sealed class FoundryAgentTests
{
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
    }

    private sealed class StubPrompts(string? text) : IPromptStore
    {
        public Task<string?> GetAsync(string name, CancellationToken ct) => Task.FromResult(text);

        public Task<IReadOnlyList<string>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<bool> SaveAsync(string name, string value, CancellationToken ct) => Task.FromResult(false);
    }

    private static readonly MemberBrief Member = new("m1", "Priya", [new BriefItem(new WorkItemId(1), "T", "Task", "Active", 3, [Flag.Stale])], "fallback");

    private static readonly PreBrief Brief = new("t1", "p\\s", "G", 6, new Burndown(10, 5, 5, 6, false), [Member], [], [], "s");

    private static FoundryChat Chat(HttpStatusCode status, string body, bool configured = true) =>
        new(
            new HttpClient(new StubHandler(status, body)) { BaseAddress = new Uri("https://example.test/") },
            Options.Create(new FoundryOptions { ProjectEndpoint = configured ? "https://example.test/api/projects/p" : string.Empty, ReasoningDeployment = "d" }),
            NullLogger<FoundryChat>.Instance);

    private static string Completion(string content) =>
        System.Text.Json.JsonSerializer.Serialize(new { choices = new[] { new { message = new { content } } } });

    private static FoundryBacklogRiskAgent BacklogAgent(FoundryChat chat, string? prompt = "instructions") =>
        new(chat, new StubPrompts(prompt), NullLogger<FoundryBacklogRiskAgent>.Instance);

    [Fact]
    public async Task Suggest_ValidReply_ParsesSummaryAndQuestions()
    {
        var reply = """{ "summary": "Two risks.", "questions": [ { "memberId": "m1", "question": "Task 1 is stale. Status?" } ] }""";

        var result = await BacklogAgent(Chat(HttpStatusCode.OK, Completion(reply))).SuggestAsync(Brief, CancellationToken.None);

        Assert.Equal("Two risks.", result!.Summary);
        Assert.Equal("Task 1 is stale. Status?", result.QuestionsByMemberId["m1"]);
    }

    [Fact]
    public async Task Suggest_ServerError_ReturnsNull()
    {
        Assert.Null(await BacklogAgent(Chat(HttpStatusCode.InternalServerError, "{}")).SuggestAsync(Brief, CancellationToken.None));
    }

    [Fact]
    public async Task Suggest_MalformedJsonContent_ReturnsNull()
    {
        Assert.Null(await BacklogAgent(Chat(HttpStatusCode.OK, Completion("not json"))).SuggestAsync(Brief, CancellationToken.None));
    }

    [Fact]
    public async Task Suggest_NotConfigured_ReturnsNull()
    {
        Assert.Null(await BacklogAgent(Chat(HttpStatusCode.OK, "{}", configured: false)).SuggestAsync(Brief, CancellationToken.None));
    }

    [Fact]
    public async Task Suggest_MissingPrompt_ReturnsNull()
    {
        Assert.Null(await BacklogAgent(Chat(HttpStatusCode.OK, "{}"), prompt: null).SuggestAsync(Brief, CancellationToken.None));
    }

    [Fact]
    public async Task Reconcile_ValidReply_ParsesQuestionAndOnlyKnownEventTypes()
    {
        var reply = """
            { "consistent": true, "mentionedWorkItemIds": [1], "risks": ["story 1 stale"], "suggestedQuestion": "Story 1 stale. Status?",
              "events": [ { "type": "commitment", "text": "Finish tomorrow", "workItemIds": [1] }, { "type": "weird", "text": "x" } ] }
            """;
        var agent = new FoundryReconcileAgent(Chat(HttpStatusCode.OK, Completion(reply)), new StubPrompts("p"));

        var result = await agent.ReconcileAsync(Member, "working on it", CancellationToken.None);

        Assert.Equal("Story 1 stale. Status?", result!.SuggestedQuestion);
        Assert.Equal(StandupEventType.Commitment, Assert.Single(result.Events).Type);
    }

    [Fact]
    public async Task Reconcile_EmptyUtterance_ReturnsNullWithoutCallingTheModel()
    {
        var agent = new FoundryReconcileAgent(Chat(HttpStatusCode.OK, "{}"), new StubPrompts("p"));

        Assert.Null(await agent.ReconcileAsync(Member, "  ", CancellationToken.None));
    }
}
