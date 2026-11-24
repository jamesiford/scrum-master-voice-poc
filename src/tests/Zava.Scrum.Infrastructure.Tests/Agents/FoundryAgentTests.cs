using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Flags;
using Zava.Scrum.Domain.Kpis;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Voice;
using Zava.Scrum.Domain.Work;
using Zava.Scrum.Infrastructure.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure.Tests.Agents;

public sealed class FoundryAgentTests
{
    /// <summary>A fake model behind a real Agent Framework agent: replies with text, or throws to simulate an outage.</summary>
    private sealed class StubAgents(string? reply, Exception? fault = null) : IFoundryAgentFactory
    {
        public string? Instructions { get; private set; }

        public int Calls { get; private set; }

        public AIAgent Create(string name, string instructions)
        {
            Instructions = instructions;
            Calls++;
            return new ChatClientAgent(new StubChatClient(reply, fault), instructions, name);
        }
    }

    private sealed class StubChatClient(string? reply, Exception? fault) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (fault is not null)
            {
                throw fault;
            }

            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
    private sealed class StubPrompts(string? text) : IPromptStore
    {
        public Task<string?> GetAsync(string name, CancellationToken ct) => Task.FromResult(text);

        public Task<IReadOnlyList<string>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<bool> SaveAsync(string name, string value, CancellationToken ct) => Task.FromResult(false);
    }

    private static readonly MemberBrief Member = new("m1", "Priya", [new BriefItem(new WorkItemId(1), "T", "Task", "Active", 3, [Flag.Stale])], "fallback");

    private static readonly PreBrief Brief = new("t1", "p\\s", "G", 6, new Burndown(10, 5, 5, 6, false), [Member], [], [], "s");

    private static FoundryChat Chat(string? reply, bool configured = true, Exception? fault = null) =>
        new(
            new StubAgents(reply, fault),
            Options.Create(new FoundryOptions { ProjectEndpoint = configured ? "https://example.test/api/projects/p" : string.Empty, ReasoningDeployment = "d" }),
            NullLogger<FoundryChat>.Instance);
    private static FoundryBacklogRiskAgent BacklogAgent(FoundryChat chat, string? prompt = "instructions") =>
        new(chat, new StubPrompts(prompt), NullLogger<FoundryBacklogRiskAgent>.Instance);

    [Fact]
    public async Task Suggest_ValidReply_ParsesSummaryAndQuestions()
    {
        var reply = """{ "summary": "Two risks.", "questions": [ { "memberId": "m1", "question": "Task 1 is stale. Status?" } ] }""";

        var result = await BacklogAgent(Chat(reply)).SuggestAsync(Brief, CancellationToken.None);

        Assert.Equal("Two risks.", result!.Summary);
        Assert.Equal("Task 1 is stale. Status?", result.QuestionsByMemberId["m1"]);
    }

    [Fact]
    public async Task Suggest_ServerError_ReturnsNull()
    {
        Assert.Null(await BacklogAgent(Chat(null, fault: new InvalidOperationException("500"))).SuggestAsync(Brief, CancellationToken.None));
    }

    [Fact]
    public async Task Suggest_MalformedJsonContent_ReturnsNull()
    {
        Assert.Null(await BacklogAgent(Chat("not json")).SuggestAsync(Brief, CancellationToken.None));
    }

    [Fact]
    public async Task Suggest_NotConfigured_ReturnsNull()
    {
        Assert.Null(await BacklogAgent(Chat("{}", configured: false)).SuggestAsync(Brief, CancellationToken.None));
    }

    [Fact]
    public async Task Suggest_MissingPrompt_ReturnsNull()
    {
        Assert.Null(await BacklogAgent(Chat("{}"), prompt: null).SuggestAsync(Brief, CancellationToken.None));
    }

    [Fact]
    public async Task Reconcile_ValidReply_ParsesQuestionAndOnlyKnownEventTypes()
    {
        var reply = """
            { "consistent": true, "mentionedWorkItemIds": [1], "risks": ["story 1 stale"], "suggestedQuestion": "Story 1 stale. Status?",
              "events": [ { "type": "commitment", "text": "Finish tomorrow", "workItemIds": [1] }, { "type": "weird", "text": "x" } ] }
            """;
        var agent = new FoundryReconcileAgent(Chat(reply), new StubPrompts("p"));

        var result = await agent.ReconcileAsync(Member, "working on it", CancellationToken.None);

        Assert.Equal("Story 1 stale. Status?", result!.SuggestedQuestion);
        Assert.Equal(StandupEventType.Commitment, Assert.Single(result.Events).Type);
    }

    [Fact]
    public async Task Reconcile_EmptyUtterance_ReturnsNullWithoutCallingTheModel()
    {
        var agent = new FoundryReconcileAgent(Chat("{}"), new StubPrompts("p"));

        Assert.Null(await agent.ReconcileAsync(Member, "  ", CancellationToken.None));
    }

    [Fact]
    public async Task Draft_ReplyAndPrompt_PassesThePromptTextAsInstructions()
    {
        var agents = new StubAgents("""{ "text": "ok" }""");
        var chat = new FoundryChat(agents, Options.Create(new FoundryOptions { ProjectEndpoint = "https://e.test/api/projects/p", ReasoningDeployment = "d" }), NullLogger<FoundryChat>.Instance);

        var reply = await new FoundryDraftingAgent(chat, new StubPrompts("be brief")).DraftJsonAsync("story-writer", "{}", CancellationToken.None);

        Assert.Equal("""{ "text": "ok" }""", reply);
        Assert.Equal("be brief", agents.Instructions);
    }

    [Fact]
    public async Task Draft_ModelOutage_ReturnsNullSoTheCallerFallsBack()
    {
        Assert.Null(await new FoundryDraftingAgent(Chat(null, fault: new HttpRequestException("down")), new StubPrompts("p")).DraftJsonAsync("x", "{}", CancellationToken.None));
    }

    [Fact]
    public async Task Narrate_ValidReply_ReturnsTheText()
    {
        var agent = new FoundryReportingAgent(Chat("""{ "text": "Velocity is steady." }"""), new StubPrompts("p"));

        Assert.Equal("Velocity is steady.", await agent.NarrateAsync("sprint", "{}", CancellationToken.None));
    }

    [Fact]
    public async Task TryCompleteJson_NotConfigured_NeverCreatesAnAgent()
    {
        var agents = new StubAgents("{}");
        var chat = new FoundryChat(agents, Options.Create(new FoundryOptions()), NullLogger<FoundryChat>.Instance);

        Assert.Null(await chat.TryCompleteJsonAsync("s", "u", CancellationToken.None));
        Assert.Equal(0, agents.Calls);
    }

    [Fact]
    public async Task TryCompleteJson_CallerCancels_StillPropagates()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Chat(null, fault: new OperationCanceledException(cts.Token)).TryCompleteJsonAsync("s", "u", cts.Token));
    }

    private static UtteranceContext Said(string text) =>
        new(text, "m1", "m1", [new NameRef("m1", "Priya")], [new BriefItem(new WorkItemId(1), "T", "Task", "Active", 3, [Flag.Stale])], null, null);

    private static FoundryUtteranceAnalyser Analyser(string? reply, string mode = "Model", bool configured = true, Exception? fault = null) =>
        new(Chat(reply, configured, fault), new StubPrompts("p"), Options.Create(new UtteranceAnalysisOptions { Mode = mode }));

    [Fact]
    public async Task ReadUtterance_ValidReply_ParsesIntentItemsAndCommentKinds()
    {
        var reply = """
            { "intent": "ask_item_info", "targetMemberId": null, "confidence": 0.85, "workItemIds": [1],
              "comments": [ { "kind": "date_change", "text": "Finish moves to Friday", "workItemIds": [1], "newDate": "Friday" },
                            { "kind": "nonsense", "text": "ignored" }, { "kind": "blocker", "text": " " } ] }
            """;

        var r = await Analyser(reply).ReadAsync(Said("is that one late?"), CancellationToken.None);

        Assert.Equal(UtteranceIntent.AskItemInfo, r!.Intent);
        Assert.Equal([1], r.WorkItemIds);
        var c = Assert.Single(r.Comments);
        Assert.Equal(CommentKind.DateChange, c.Kind);
        Assert.Equal("Friday", c.NewDate);
        Assert.Equal(0.85, r.Confidence);
    }

    [Fact]
    public async Task ReadUtterance_UnknownIntent_LeavesItNullSoTheRulesDecide()
    {
        var r = await Analyser("""{ "intent": "dance", "confidence": 0.9 }""").ReadAsync(Said("hi"), CancellationToken.None);

        Assert.Null(r!.Intent);
    }

    [Theory]
    [InlineData("Rules", true)]
    [InlineData("Model", false)]
    public async Task ReadUtterance_RulesModeOrNotConfigured_NeverCallsTheModel(string mode, bool configured)
    {
        Assert.Null(await Analyser("{}", mode, configured).ReadAsync(Said("hi"), CancellationToken.None));
    }

    [Fact]
    public async Task ReadUtterance_ModelFaultOrBadJson_ReturnsNull()
    {
        Assert.Null(await Analyser(null, fault: new InvalidOperationException("429")).ReadAsync(Said("hi"), CancellationToken.None));
        Assert.Null(await Analyser("not json").ReadAsync(Said("hi"), CancellationToken.None));
    }
}
