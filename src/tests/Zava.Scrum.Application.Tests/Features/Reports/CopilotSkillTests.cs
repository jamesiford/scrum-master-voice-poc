using Microsoft.Extensions.DependencyInjection;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Reports.AnswerCopilotQuestion;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Reports;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Application.Tests.Features.Reports;

public sealed class CopilotSkillTests
{
    private static readonly Guid AskerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Member AskerMember = new("m-priya", "t1", "priya", "Priya", "priya@zava.test", "", 1, true, EntraObjectId: AskerId);

    [Theory]
    [InlineData(CopilotSkill.Blocked, "#12")]
    [InlineData(CopilotSkill.Commitments, "Finish #1")]
    [InlineData(CopilotSkill.SprintRisk, "#1")]
    [InlineData(CopilotSkill.StandupStatus, "Prepared")]
    [InlineData(CopilotSkill.MyItems, "#1")]
    public async Task Answer_SupportedSkill_UsesRealMediatorAndOnlyAskerAccess(CopilotSkill skill, string expected)
    {
        var reader = new Reader();
        await using var h = await CreateAsync(reader);
        var actor = Who.Actor(AskerId, "Priya", Domain.Identity.ActorSource.Teams);
        var session = await h.LoadAsync();
        session.RecordEvent(StandupEventType.Commitment, AskerMember.Id, [1], "Finish #1", StandupHarness.Now);

        var result = await h.Sender.Send(new AnswerCopilotQuestionQuery(actor, skill));

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Contains(expected, result.Value.Text, StringComparison.Ordinal);
        Assert.Equal($"ado-for-{AskerId}", reader.Token);
        Assert.Equal("t1", reader.TeamId);
        Assert.All(result.Value.ItemLinks, link => Assert.StartsWith("https://dev.azure.com/zava/p/_workitems/edit/", link, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Answer_UserWithoutAdoAccess_RefusesWithoutModelOrServiceReader()
    {
        var reader = new Reader { Denied = true };
        var phraser = new Phraser();
        await using var h = await CreateAsync(reader, phraser);
        var actor = Who.Actor(AskerId, "Priya", Domain.Identity.ActorSource.Teams);

        var result = await h.Sender.Send(new AnswerCopilotQuestionQuery(actor, CopilotSkill.Blocked));

        Assert.Equal("copilot.ado_denied", result.Error!.Code);
        Assert.Contains("your access", result.Error.Message, StringComparison.Ordinal);
        Assert.Equal(0, phraser.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Answer_MissingOrDifferentAssertion_RefusesBeforeReading(bool mismatched)
    {
        var reader = new Reader();
        await using var h = await CreateAsync(reader);
        var actor = Who.Actor(AskerId, "Priya", Domain.Identity.ActorSource.Teams);
        Who.Signed = mismatched ? new UserAssertion("other", Who.AdminId, Who.Tenant) : null;

        var result = await h.Sender.Send(new AnswerCopilotQuestionQuery(actor, CopilotSkill.Blocked));

        Assert.Equal("copilot.sign_in_required", result.Error!.Code);
        Assert.Null(reader.Token);
    }

    [Fact]
    public async Task Answer_NoMembership_RefusesEvenForAdministrator()
    {
        var reader = new Reader();
        await using var h = await CreateAsync(reader);
        var actor = Who.Actor(Who.AdminId, "Admin", Domain.Identity.ActorSource.Teams);

        var result = await h.Sender.Send(new AnswerCopilotQuestionQuery(actor, CopilotSkill.Blocked));

        Assert.Equal("copilot.no_team", result.Error!.Code);
        Assert.Null(reader.Token);
    }

    [Fact]
    public async Task Answer_MultipleMemberships_RequiresExplicitChoiceAndReauthorisesIt()
    {
        var reader = new Reader();
        await using var h = await CreateAsync(reader);
        var actor = Who.Actor(AskerId, "Priya", Domain.Identity.ActorSource.Teams);
        await h.Get<ITeamRepository>().SaveAsync(Team("t2"), default);
        await h.Get<ITeamMembershipRepository>().UpsertAsync(new("t2", AskerId, TeamRole.Member), default);

        var ambiguous = await h.Sender.Send(new AnswerCopilotQuestionQuery(actor, CopilotSkill.Blocked));
        var forbidden = await h.Sender.Send(new AnswerCopilotQuestionQuery(actor, CopilotSkill.Blocked, "other"));
        Assert.Equal("team.ambiguous", ambiguous.Error!.Code);
        Assert.Contains("team <id>", ambiguous.Error.Message, StringComparison.Ordinal);
        Assert.False(forbidden.IsSuccess);
        Assert.Null(reader.Token);

        var chosen = await h.Sender.Send(new AnswerCopilotQuestionQuery(actor, CopilotSkill.Blocked, "t2"));
        Assert.True(chosen.IsSuccess);
        Assert.Equal("t2", reader.TeamId);
    }

    [Fact]
    public async Task Answer_ConsentFailure_RefusesBeforeSprintRead()
    {
        var reader = new Reader();
        await using var h = await CreateAsync(reader);
        var actor = Who.Actor(AskerId, "Priya", Domain.Identity.ActorSource.Teams);
        var tokens = (FakeApproverTokenSource)h.Get<IApproverTokenSource>();
        tokens.Refuse = true;

        var result = await h.Sender.Send(new AnswerCopilotQuestionQuery(actor, CopilotSkill.Blocked));

        Assert.Equal("delegation.exchange_failed", result.Error!.Code);
        Assert.Null(reader.Token);
    }

    [Theory]
    [InlineData("There are 999 blocked items.")]
    [InlineData("See #999.")]
    [InlineData("You have three items.")]
    [InlineData("Open https://evil.test")]
    public async Task Answer_UngroundedModelIntroduction_DropsItBeforeOutput(string text)
    {
        await using var h = await CreateAsync(new Reader(), new Phraser { Text = text });
        var actor = Who.Actor(AskerId, "Priya", Domain.Identity.ActorSource.Teams);

        var result = await h.Sender.Send(new AnswerCopilotQuestionQuery(actor, CopilotSkill.Blocked));

        Assert.False(result.Value.FromModel);
        Assert.DoesNotContain(text, result.Value.Text, StringComparison.Ordinal);
        Assert.Contains("1 readable sprint item is", result.Value.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Answer_UnverifiedRoster_MyItemsRefusesInsteadOfUsingDisplayNameOrDemoTag()
    {
        await using var h = await CreateAsync(new Reader());
        var actor = Who.Actor(AskerId, "Priya", Domain.Identity.ActorSource.Teams);
        await h.Get<IMemberRepository>().ReplaceAsync("t1", [AskerMember with { EntraObjectId = null }], default);

        var result = await h.Sender.Send(new AnswerCopilotQuestionQuery(actor, CopilotSkill.MyItems));

        Assert.Equal("copilot.roster_required", result.Error!.Code);
    }

    private static Team Team(string id) => new(id, id, "UTC", 15, new("https://dev.azure.com/zava", "p", "t", "p"), new TeamBehaviours(), "Ship");

    private static async Task<StandupHarness> CreateAsync(Reader reader, Phraser? phraser = null)
    {
        var h = await StandupHarness.CreateAsync(configure: services =>
        {
            services.AddSingleton<IDelegatedWorkItemReader>(reader);
            services.AddSingleton<ICopilotPhraser>(phraser ?? new Phraser());
        });
        await h.Get<ITeamRepository>().SaveAsync(Team("t1"), default);
        await h.Get<ITeamMembershipRepository>().UpsertAsync(new("t1", AskerId, TeamRole.Member), default);
        await h.Get<IMemberRepository>().ReplaceAsync("t1", [AskerMember], default);
        return h;
    }

    private sealed class Reader : IDelegatedWorkItemReader
    {
        public bool Denied { get; init; }
        public string? Token { get; private set; }
        public string? TeamId { get; private set; }

        public Task<Result<SprintSnapshot>> GetCurrentSprintAsync(Team team, DelegatedAdoAccess access, CancellationToken ct)
        {
            Token = access.AccessToken;
            TeamId = team.Id;
            var now = StandupHarness.Now;
            WorkItem[] items =
            [
                new(new(1), "Retry", "Task", "New", AskerMember.AdoUniqueName, null, now, now, null, 5, now.AddHours(2), []),
                new(new(12), "Timeout", "Bug", "Active", "other@zava.test", "priya", now, now, 3, null, null, ["blocked"]),
            ];
            return Task.FromResult(Denied ? Result<SprintSnapshot>.Failure(CopilotErrors.AccessDenied)
                : Result<SprintSnapshot>.Success(new("p\\s", now.AddDays(-1), now.AddDays(5), items)));
        }
    }

    private sealed class Phraser : ICopilotPhraser
    {
        public string? Text { get; init; } = "Here is the information available with your access.";
        public int Calls { get; private set; }

        public Task<string?> PhraseAsync(string summary, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(Text);
        }
    }
}
