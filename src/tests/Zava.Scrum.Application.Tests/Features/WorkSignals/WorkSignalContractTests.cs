using Microsoft.Extensions.DependencyInjection;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.WorkSignals;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;
using Zava.Scrum.Domain.WorkSignals;

namespace Zava.Scrum.Application.Tests.Features.WorkSignals;

public sealed class WorkSignalContractTests
{
    private static readonly Guid Owner = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Correlation = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly WorkSignalScope Scope = new("t1", "group", "channel", ["meeting"], [new(12)], [Owner, Who.AdminId]);
    private static WorkSignalEvidence Evidence => new(WorkSignalSource.Channel, "Channel update",
        new("https://teams.microsoft.com/l/message/channel/1"), StandupHarness.Now, "Retry fix is ready.",
        "channel", Owner, "group");

    [Theory]
    [InlineData("brief")]
    [InlineData("update")]
    [InlineData("helpers")]
    [InlineData("review")]
    public async Task Read_AdvisoryContractCases_ReturnsCitationsAndNeverWrites(string use)
    {
        var port = new Signals
        {
            Evidence = use == "review" ? [Evidence with { Source = WorkSignalSource.Meeting, ResourceId = "meeting" }] : [Evidence],
        };
        await using var h = await CreateAsync(port);

        var result = await h.Sender.Send(new ProbeQuery(use, "t1", 12, "meeting"));

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(Evidence.Snippet, result.Value.Single().Snippet);
        Assert.Equal(Owner, port.Context!.OwnerAssertion.ObjectId);
        Assert.Equal(Correlation, port.Context.CorrelationId);
        Assert.Equal("t1", port.Context.Team.Id);
        Assert.Empty(await h.Get<IFollowUpRepository>().ListAsync("t1", null, default));
        Assert.Equal(1, port.Calls);
    }

    [Theory]
    [InlineData("off")]
    [InlineData("revoked")]
    [InlineData("missing-consent")]
    [InlineData("removed")]
    [InlineData("demoted")]
    [InlineData("other-owner")]
    [InlineData("missing-assertion")]
    [InlineData("other-tenant")]
    public async Task Read_DisabledOrUnauthorised_MakesZeroProviderAndScopeCalls(string state)
    {
        var port = new Signals();
        var consents = new Consents();
        var scopes = new Scopes();
        await using var h = await CreateAsync(port, consents, scopes);
        if (state == "off")
        {
            var repo = h.Get<ITeamRepository>();
            await repo.SaveAsync((await repo.GetAsync("t1", default))! with { WorkSignalsEnabled = false }, default);
        }
        else if (state == "revoked")
        {
            consents.Current = consents.Current!.Revoke(Owner, false, StandupHarness.Now).Value;
        }
        else if (state == "missing-consent")
        {
            consents.Current = null;
        }
        else if (state == "removed")
        {
            await h.Get<ITeamMembershipRepository>().RemoveAsync("t1", Owner, default);
        }
        else if (state == "demoted")
        {
            await h.Get<ITeamMembershipRepository>().UpsertAsync(new("t1", Owner, TeamRole.Member), default);
        }
        else if (state == "other-owner")
        {
            consents.Current = consents.Current! with { OwnerObjectId = Who.AdminId };
        }
        else
        {
            h.Get<IUserAssertionAccessor>().Current = state == "missing-assertion" ? null : new("assertion", Owner, Guid.NewGuid());
        }

        var result = await h.Sender.Send(new ProbeQuery("brief", "t1", 12, "meeting"));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, port.Calls);
        Assert.Equal(0, scopes.Calls);
    }

    [Fact]
    public async Task Read_OwnerNotOnOtherTeam_DoesNotRevealSignalsEvenForAdmin()
    {
        var port = new Signals();
        await using var h = await CreateAsync(port);
        await h.Get<ITeamRepository>().SaveAsync(new("other", "Other", "UTC", 15, new("o", "p", "t", "a"), new(), "", WorkSignalsEnabled: true), default);

        var result = await h.Sender.Send(new ProbeQuery("brief", "other", 12, "meeting"));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, port.Calls);
    }

    [Theory]
    [InlineData("brief", 999, "meeting")]
    [InlineData("helpers", 999, "meeting")]
    [InlineData("review", 12, "other")]
    public async Task Read_ItemOrMeetingOutsideTeam_MakesZeroProviderCalls(string use, int item, string meeting)
    {
        var port = new Signals();
        await using var h = await CreateAsync(port);

        var result = await h.Sender.Send(new ProbeQuery(use, "t1", item, meeting));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, port.Calls);
    }

    [Fact]
    public async Task Read_ExpiredToken_PropagatesVisibleFailureWithoutFallback()
    {
        var port = new Signals { Failure = WorkSignalAccess.Unavailable };
        await using var h = await CreateAsync(port);

        var result = await h.Sender.Send(new ProbeQuery("brief", "t1", 12, "meeting"));

        Assert.Equal("signals.unavailable", result.Error!.Code);
        Assert.Contains("sign in again", result.Error.Message, StringComparison.Ordinal);
        Assert.Equal(1, port.Calls);
    }

    [Fact]
    public async Task Read_ConsentRevokedDuringProviderCall_DoesNotReturnEvidence()
    {
        var consents = new Consents();
        var port = new Signals { DuringRead = () => consents.Current = consents.Current!.Revoke(Owner, false, StandupHarness.Now).Value };
        await using var h = await CreateAsync(port, consents);

        var result = await h.Sender.Send(new ProbeQuery("brief", "t1", 12, "meeting"));

        Assert.Equal("signals.unavailable", result.Error!.Code);
        Assert.Equal(1, port.Calls);
    }

    [Fact]
    public async Task Read_ScopeForDifferentTeam_RefusesBeforeProvider()
    {
        var port = new Signals();
        await using var h = await CreateAsync(port, scopes: new Scopes { Value = Scope with { TeamId = "other" } });

        var result = await h.Sender.Send(new ProbeQuery("brief", "t1", 12, "meeting"));

        Assert.Equal("signals.scope_invalid", result.Error!.Code);
        Assert.Equal(0, port.Calls);
    }

    [Theory]
    [InlineData("other-channel")]
    [InlineData("other-group")]
    [InlineData("unknown-person")]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("oversized")]
    [InlineData("unsafe-link")]
    [InlineData("wrong-source")]
    public async Task Read_UnverifiedEvidence_RejectsInsteadOfLeakingOrReturningEmptySuccess(string state)
    {
        var evidence = state switch
        {
            "other-channel" => Evidence with { ResourceId = "other" },
            "other-group" => Evidence with { TeamsGroupId = "other" },
            "unknown-person" => Evidence with { MemberObjectId = Guid.NewGuid() },
            "expired" => Evidence with { Timestamp = StandupHarness.Now.AddDays(-7) },
            "future" => Evidence with { Timestamp = StandupHarness.Now.AddSeconds(1) },
            "oversized" => Evidence with { Snippet = new string('x', 501) },
            "unsafe-link" => Evidence with { Link = new("http://teams.microsoft.com/message") },
            _ => Evidence with { Source = WorkSignalSource.Meeting },
        };
        await using var h = await CreateAsync(new Signals { Evidence = [evidence] });

        var result = await h.Sender.Send(new ProbeQuery("brief", "t1", 12, "meeting"));

        Assert.Equal("signals.evidence_rejected", result.Error!.Code);
    }

    [Fact]
    public async Task SuggestHelpers_DuplicateOrUnnamedAuthors_ReturnsDistinctVerifiedPeopleOnly()
    {
        var port = new Signals { Evidence = [Evidence, Evidence, Evidence with { MemberObjectId = null }, Evidence with { MemberObjectId = Who.AdminId }] };
        await using var h = await CreateAsync(port);

        var result = await h.Sender.Send(new ProbeQuery("helpers", "t1", 12, "meeting"));

        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, e => Assert.NotNull(e.MemberObjectId));
    }

    [Fact]
    public async Task SuggestHelpers_FourVerifiedAuthors_ReturnsAtMostThree()
    {
        Guid[] authors = [Owner, Who.AdminId, Guid.NewGuid(), Guid.NewGuid()];
        var port = new Signals { Evidence = authors.Select(id => Evidence with { MemberObjectId = id }).ToList() };
        await using var h = await CreateAsync(port, scopes: new Scopes { Value = Scope with { RosterObjectIds = authors } });

        var result = await h.Sender.Send(new ProbeQuery("helpers", "t1", 12, "meeting"));

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(3, result.Value.Count);
    }

    [Theory]
    [InlineData("helpers")]
    [InlineData("review")]
    public async Task Read_TeamFlagOffForEveryOperation_MakesZeroProviderCalls(string use)
    {
        var port = new Signals();
        var scopes = new Scopes();
        await using var h = await CreateAsync(port, scopes: scopes);
        var teams = h.Get<ITeamRepository>();
        await teams.SaveAsync((await teams.GetAsync("t1", default))! with { WorkSignalsEnabled = false }, default);

        var result = await h.Sender.Send(new ProbeQuery(use, "t1", 12, "meeting"));

        Assert.Equal("signals.unavailable", result.Error!.Code);
        Assert.Equal(0, port.Calls);
        Assert.Equal(0, scopes.Calls);
    }

    [Theory]
    [InlineData(0, 500)]
    [InlineData(31, 500)]
    [InlineData(7, 0)]
    [InlineData(7, 1001)]
    public void Validate_InvalidRetentionOrExcerptLimit_Refuses(int days, int characters)
    {
        var policy = new WorkSignalEvidencePolicy(TimeSpan.FromDays(days), characters);

        var result = policy.Validate([Evidence], Scope, WorkSignalSource.Channel, "channel", StandupHarness.Now);

        Assert.Equal("signals.policy_invalid", result.Error!.Code);
    }

    [Fact]
    public async Task Read_EmptySuccessfulProviderResponse_RemainsDistinctFromUnavailable()
    {
        await using var h = await CreateAsync(new Signals { Evidence = [] });

        var result = await h.Sender.Send(new ProbeQuery("brief", "t1", 12, "meeting"));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    private static async Task<StandupHarness> CreateAsync(Signals port, Consents? consents = null, Scopes? scopes = null)
    {
        var h = await StandupHarness.CreateAsync(configure: services =>
        {
            services.AddSingleton<IUserAssertionAccessor>(new UserAssertionAccessor { Current = new("owner-assertion", Owner, Who.Tenant) });
            services.AddSingleton<IIdentityPolicy>(new FakeIdentityPolicy(Who.Tenant, Owner));
            services.AddSingleton<IWorkSignals>(port);
            services.AddSingleton<IWorkSignalConsentRepository>(consents ?? new Consents());
            services.AddSingleton<IWorkSignalScopeReader>(scopes ?? new Scopes());
            services.AddSingleton(new WorkSignalEvidencePolicy(TimeSpan.FromDays(7), 500));
            services.AddScoped<WorkSignalReader>();
            services.AddScoped<IRequestHandler<ProbeQuery, Result<IReadOnlyList<WorkSignalEvidence>>>, ProbeHandler>();
        });
        await h.Get<ITeamRepository>().SaveAsync(new("t1", "Team", "UTC", 15, new("o", "p", "t", "a"), new(), "", WorkSignalsEnabled: true), default);
        await h.Get<ITeamMembershipRepository>().UpsertAsync(new("t1", Owner, TeamRole.TeamLead), default);
        return h;
    }

    // Test-only presentation entry: the production mediator has no Work IQ route or unverified adapter.
    private sealed record ProbeQuery(string Use, string TeamId, int Item, string Meeting) : IQuery<IReadOnlyList<WorkSignalEvidence>>;

    private sealed class ProbeHandler(WorkSignalReader reader) : IRequestHandler<ProbeQuery, Result<IReadOnlyList<WorkSignalEvidence>>>
    {
        public Task<Result<IReadOnlyList<WorkSignalEvidence>>> Handle(ProbeQuery request, CancellationToken ct) => request.Use switch
        {
            "helpers" => reader.SuggestHelpersAsync(request.TeamId, new(request.Item), "retry", Correlation, ct),
            "review" => reader.GetMeetingNotesAsync(request.TeamId, request.Meeting, Correlation, ct),
            _ => reader.GetChannelActivityAsync(request.TeamId, new(request.Item), Correlation, ct),
        };
    }

    private sealed class Consents : IWorkSignalConsentRepository
    {
        public WorkSignalConsent? Current { get; set; } = new("t1", Owner, Who.Tenant, StandupHarness.Now);
        public Task<WorkSignalConsent?> GetAsync(string teamId, CancellationToken ct) => Task.FromResult(Current?.TeamId == teamId ? Current : null);
        public Task SaveAsync(WorkSignalConsent consent, CancellationToken ct)
        {
            Current = consent;
            return Task.CompletedTask;
        }
    }

    private sealed class Scopes : IWorkSignalScopeReader
    {
        public WorkSignalScope Value { get; init; } = Scope;
        public int Calls { get; private set; }
        public Task<Result<WorkSignalScope>> GetAsync(Team team, UserAssertion owner, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(Result<WorkSignalScope>.Success(Value));
        }
    }

    private sealed class Signals : IWorkSignals
    {
        public IReadOnlyList<WorkSignalEvidence> Evidence { get; init; } = [WorkSignalContractTests.Evidence];
        public Error? Failure { get; init; }
        public Action? DuringRead { get; init; }
        public int Calls { get; private set; }
        public WorkSignalContext? Context { get; private set; }
        public Task<Result<IReadOnlyList<WorkSignalEvidence>>> GetChannelActivityAsync(WorkSignalContext context, WorkItemId workItem, CancellationToken ct) => Read(context);
        public Task<Result<IReadOnlyList<WorkSignalEvidence>>> SuggestHelpersAsync(WorkSignalContext context, string topic, CancellationToken ct) => Read(context);
        public Task<Result<IReadOnlyList<WorkSignalEvidence>>> GetMeetingNotesAsync(WorkSignalContext context, string meetingId, CancellationToken ct) => Read(context);
        private Task<Result<IReadOnlyList<WorkSignalEvidence>>> Read(WorkSignalContext context)
        {
            Calls++;
            Context = context;
            DuringRead?.Invoke();
            return Task.FromResult(Failure is null ? Result<IReadOnlyList<WorkSignalEvidence>>.Success(Evidence)
                : Result<IReadOnlyList<WorkSignalEvidence>>.Failure(Failure));
        }
    }
}
