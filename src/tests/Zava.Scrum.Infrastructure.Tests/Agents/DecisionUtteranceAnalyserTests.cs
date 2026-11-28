using System.Net;
using System.Text;
using System.Text.Json;
using Zava.Scrum.Domain.Flags;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Voice;
using Zava.Scrum.Domain.Work;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Infrastructure.Agents;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Zava.Scrum.Infrastructure.Persistence;

namespace Zava.Scrum.Infrastructure.Tests.Agents;

public sealed class DecisionUtteranceAnalyserTests
{
    private sealed class Stub(HttpStatusCode status, string body, TimeSpan? delay = null) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public string? LastRequest { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastRequest = await request.Content!.ReadAsStringAsync(cancellationToken);
            if (delay is { } d)
            {
                await Task.Delay(d, cancellationToken);
            }

            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private static readonly NameRef[] Roster = [new("m1", "Priya"), new("m2", "Marcus")];

    private static readonly BriefItem[] Items =
    [
        new(new WorkItemId(1), "Payment retry", "User Story", "Active", 3, [Flag.Stale]),
        new(new WorkItemId(14), "Retry load test", "Task", "Active", 1, []),
    ];

    private static UtteranceContext Said(string text, string speaker = "m1") => new(text, speaker, "m1", Roster, Items, null, null) { TeamId = "team-test" };

    private static DecisionUtteranceAnalyser Analyser(Stub stub, string engine = "Decision", string deployment = "zava-decision", int timeoutMs = 2000) =>
        new(
            new HttpClient(stub) { BaseAddress = new Uri("https://example.test/") },
            Options.Create(new FoundryOptions { ProjectEndpoint = "https://example.test/api/projects/p", ReasoningDeployment = "d" }),
            Options.Create(new UtteranceAnalysisOptions { Engine = engine, DecisionDeployment = deployment, TimeoutMs = timeoutMs }),
            new FilePromptStore(new FileCollectionStore(Options.Create(new StoreOptions { DataPath = Path.Combine(Path.GetTempPath(), "decision-tests-" + Guid.NewGuid().ToString("N")) }))),
            NullLogger<DecisionUtteranceAnalyser>.Instance);

    private static string Reply(string intent, double confidence, string named = "none", params (string Question, double P)[] yes)
    {
        var answers = new Dictionary<string, object>
        {
            ["intent"] = new { type = "choice", choice = intent, confidence },
            ["named"] = new { type = "choice", choice = named, confidence = 0.9 },
        };
        foreach (var (q, p) in yes)
        {
            answers[q] = new { type = "noul", noul = p };
        }

        return JsonSerializer.Serialize(new { model = "microsoft-decision-1.1", answers });
    }

    [Fact]
    public async Task ReadWithEvidence_SubThresholdProbabilities_PreservesCalibrationEvidenceWithoutChangingReading()
    {
        var stub = new Stub(HttpStatusCode.OK, Reply("update", 0.59, yes: [("item_1", 0.69), ("kind_Blocker", 0.7)]));

        var evidence = await Analyser(stub).ReadWithEvidenceAsync(Said("blocked"), CancellationToken.None);

        Assert.Null(evidence.Failure);
        Assert.Equal(0.59, evidence.Probabilities["intent"]);
        Assert.Equal(0.69, evidence.Probabilities["item_1"]);
        Assert.Equal(0.7, evidence.Probabilities["kind_Blocker"]);
        Assert.Empty(evidence.Reading!.WorkItemIds);
        Assert.Single(evidence.Reading.Comments);
    }

    [Fact]
    public async Task ReadWithEvidence_HttpRefusal_ReportsFailureRatherThanZeroConfidence()
    {
        var evidence = await Analyser(new Stub(HttpStatusCode.Forbidden, "{}")).ReadWithEvidenceAsync(Said("hi"), CancellationToken.None);

        Assert.Equal("http_403", evidence.Failure);
        Assert.Null(evidence.Reading);
        Assert.Empty(evidence.Probabilities);
    }

    [Fact]
    public async Task Read_Update_ReturnsIntentItemsAndCommentKindsFromProbabilities()
    {
        var stub = new Stub(HttpStatusCode.OK, Reply("update", 0.93, yes: [("item_1", 0.95), ("item_14", 0.1), ("kind_Blocker", 0.99), ("kind_DateChange", 0.9), ("kind_Commitment", 0.2)]));

        var r = await Analyser(stub).ReadAsync(Said("stuck on the retry story, so it slips to Friday"), CancellationToken.None);

        Assert.Equal(UtteranceIntent.Update, r!.Intent);
        Assert.Equal(0.93, r.Confidence);
        Assert.Equal([1], r.WorkItemIds);
        Assert.Equal([CommentKind.Blocker, CommentKind.DateChange], r.Comments.Select(c => c.Kind));
        Assert.All(r.Comments, c => Assert.Equal([1], c.WorkItemIds));
        Assert.Equal("Friday", r.Comments[1].NewDate);
        Assert.Contains("\"model\":\"zava-decision\"", stub.LastRequest);
        Assert.Contains("item_14", stub.LastRequest);
    }

    [Fact]
    public async Task Read_Question_RecordsNoComments()
    {
        var stub = new Stub(HttpStatusCode.OK, Reply("ask_item_info", 0.9, yes: [("kind_Blocker", 0.95)]));

        var r = await Analyser(stub).ReadAsync(Said("is that one blocked?"), CancellationToken.None);

        Assert.Equal(UtteranceIntent.AskItemInfo, r!.Intent);
        Assert.Empty(r.Comments);
    }

    [Theory]
    [InlineData("date_1", "Monday")]
    [InlineData("none", null)]
    [InlineData("2026-11-30", null)]
    public async Task Read_MultipleDates_UsesOnlyBoundedVerbatimRevisedChoice(string selected, string? expected)
    {
        var reply = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Reply("update", 0.9, yes: [("item_1", 0.95), ("kind_DateChange", 0.95)]))!;
        var answers = JsonSerializer.Deserialize<Dictionary<string, object>>(reply["answers"])!;
        answers["revised_date"] = new { choice = selected, confidence = 0.95 };
        var stub = new Stub(HttpStatusCode.OK, JsonSerializer.Serialize(new { answers }));
        var context = Said("Payment retry slips from Friday to Monday") with
        {
            DateContext = new SpokenDateContext(new DateOnly(2026, 11, 26)),
        };

        var reading = await Analyser(stub).ReadAsync(context, CancellationToken.None);
        var comment = Assert.Single(reading!.Comments);

        Assert.Equal(expected, comment.NewDate);
        Assert.Equal(expected is null ? null : new DateOnly(2026, 11, 30), comment.DateResolution!.Date);
        Assert.Contains("\"revised_date\"", stub.LastRequest);
        Assert.Contains("\"date_0\"", stub.LastRequest);
        Assert.Contains("\"date_1\"", stub.LastRequest);
    }

    [Fact]
    public async Task Read_DateChoiceWithMissingConfidence_DoesNotAssumeCertainDate()
    {
        const string body = """{"answers":{"intent":{"choice":"update","confidence":0.9},"kind_DateChange":{"noul":0.9},"revised_date":{"choice":"date_1"}}}""";
        var reading = await Analyser(new Stub(HttpStatusCode.OK, body)).ReadAsync(Said("It moves from Friday to Monday"), CancellationToken.None);

        Assert.Null(Assert.Single(reading!.Comments).NewDate);
    }

    [Fact]
    public async Task Read_MissingTeam_ReportsMissingInstructionsAndDoesNotCallService()
    {
        var stub = new Stub(HttpStatusCode.OK, Reply("update", 0.9));
        var evidence = await Analyser(stub).ReadWithEvidenceAsync(Said("hi") with { TeamId = "" }, CancellationToken.None);

        Assert.Equal("instructions_unavailable", evidence.Failure);
        Assert.Null(evidence.Reading);
        Assert.Equal(0, stub.Calls);
    }

    [Fact]
    public async Task Read_MultipleDatesWithoutChoice_DoesNotSilentlyUseOldDate()
    {
        var stub = new Stub(HttpStatusCode.OK, Reply("update", 0.9, yes: [("kind_DateChange", 0.95)]));

        var reading = await Analyser(stub).ReadAsync(Said("It moves from Friday to Monday"), CancellationToken.None);

        Assert.Null(Assert.Single(reading!.Comments).NewDate);
    }

    [Fact]
    public async Task Read_StepAwayNamingATeammate_TargetsThatTeammate()
    {
        var stub = new Stub(HttpStatusCode.OK, Reply("step_away", 0.9, named: "m2"));

        var r = await Analyser(stub).ReadAsync(Said("Marcus is out for a call"), CancellationToken.None);

        Assert.Equal("m2", r!.TargetMemberId);
    }

    [Theory]
    [InlineData(0.95, "m1", "m2")]
    [InlineData(0.1, "m2", "m1")]
    public async Task Read_Proxy_UsesTheHandOverAnswerToPickWhoSpeaksForWhom(double handsOver, string target, string by)
    {
        var stub = new Stub(HttpStatusCode.OK, Reply("proxy_update", 0.9, named: "m2", yes: [("hands_over", handsOver)]));

        var r = await Analyser(stub).ReadAsync(Said("Marcus will do the update"), CancellationToken.None);

        Assert.Equal(target, r!.TargetMemberId);
        Assert.Equal(by, r.ByMemberId);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Read_HttpError_ReturnsNull(HttpStatusCode status)
    {
        Assert.Null(await Analyser(new Stub(status, """{"error":{"code":"x"}}""")).ReadAsync(Said("hi"), CancellationToken.None));
    }

    [Fact]
    public async Task Read_BadBodyOrUnknownIntent_ReturnsNull()
    {
        Assert.Null(await Analyser(new Stub(HttpStatusCode.OK, "not json")).ReadAsync(Said("hi"), CancellationToken.None));
        Assert.Null(await Analyser(new Stub(HttpStatusCode.OK, Reply("dance", 0.9))).ReadAsync(Said("hi"), CancellationToken.None));
    }

    [Fact]
    public async Task Read_OverTheLatencyBudget_ReturnsNull()
    {
        var stub = new Stub(HttpStatusCode.OK, Reply("update", 0.9), delay: TimeSpan.FromSeconds(5));

        Assert.Null(await Analyser(stub, timeoutMs: 200).ReadAsync(Said("hi"), CancellationToken.None));
    }

    [Theory]
    [InlineData("Chat", "zava-decision")]
    [InlineData("Decision", "")]
    public async Task Read_ChatEngineOrNoDeployment_NeverCallsTheService(string engine, string deployment)
    {
        var stub = new Stub(HttpStatusCode.OK, Reply("update", 0.9));

        Assert.Null(await Analyser(stub, engine, deployment).ReadAsync(Said("hi"), CancellationToken.None));
        Assert.Equal(0, stub.Calls);
    }
}
