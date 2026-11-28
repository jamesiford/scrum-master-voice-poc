using Microsoft.Extensions.DependencyInjection;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Demo.GetDemoReseed;
using Zava.Scrum.Application.Features.Demo.AbortDemoReseed;
using Zava.Scrum.Application.Features.Demo.ReseedDemo;
using Zava.Scrum.Application.Features.Demo.ResetDemo;
using Zava.Scrum.Application.Features.Demo.OptInDemoScope;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Identity;

namespace Zava.Scrum.Application.Tests.Features.Demo;

/// <summary>Exercises Admin enforcement and stage/commit dispatch through the real mediator and pipeline.</summary>
public sealed class ReseedDemoTests
{
    [Fact]
    public async Task Send_NonAdmin_RefusesBeforePersistenceForCommandsAndQueries()
    {
        var reset = new Reset();
        await using var provider = Provider(reset);
        var sender = provider.GetRequiredService<ISender>();
        var decision = Who.Portal();

        var stage = await sender.Send(new ReseedDemoCommand("demo", decision));
        var commit = await sender.Send(new ReseedDemoCommand("demo", decision, reset.Id));
        var query = await sender.Send(new GetDemoReseedQuery("demo", reset.Id, decision));
        var abort = await sender.Send(new AbortDemoReseedCommand("demo", reset.Id, decision));
        var legacy = await sender.Send(new ResetDemoCommand(decision));
        var anonymousLegacy = await sender.Send(new ResetDemoCommand());
        var optIn = await sender.Send(new OptInDemoScopeCommand(new DemoResetScopeManifest(Who.Tenant, "org", [], [], []), decision));

        Assert.Equal("demo.admin_required", stage.Error!.Code);
        Assert.Equal("demo.admin_required", commit.Error!.Code);
        Assert.Equal("demo.admin_required", query.Error!.Code);
        Assert.Equal("demo.admin_required", abort.Error!.Code);
        Assert.Equal("demo.admin_required", legacy.Error!.Code);
        Assert.Equal("demo.admin_required", anonymousLegacy.Error!.Code);
        Assert.Equal("demo.admin_required", optIn.Error!.Code);
        Assert.Empty(reset.Calls);
    }

    [Fact]
    public async Task Send_Admin_PreservesTeamActorAndStableOperationForRecovery()
    {
        var reset = new Reset();
        await using var provider = Provider(reset);
        var sender = provider.GetRequiredService<ISender>();
        var decision = Who.Portal("Dana", AppRoles.Admin);

        var stage = await sender.Send(new ReseedDemoCommand("demo", decision));
        var query = await sender.Send(new GetDemoReseedQuery("demo", stage.Value, decision));
        var commit = await sender.Send(new ReseedDemoCommand("demo", decision, stage.Value));

        Assert.Equal(reset.Id, commit.Value);
        Assert.Equal(reset.Id, query.Value.OperationId);
        Assert.Equal(["stage:demo", "get:demo", "commit:demo"], reset.Calls);
        Assert.Equal(decision.Actor, reset.Actor);
    }

    private static ServiceProvider Provider(Reset reset)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IDemoReseed>(reset);
        return services.BuildServiceProvider();
    }

    private sealed class Reset : IDemoReseed
    {
        public Guid Id { get; } = Guid.NewGuid();
        public List<string> Calls { get; } = [];
        public ActorIdentity? Actor { get; private set; }
        public Task<Result<Guid>> StageAsync(string teamId, ActorIdentity actor, CancellationToken ct)
        {
            Calls.Add("stage:" + teamId);
            Actor = actor;
            return Task.FromResult(Result<Guid>.Success(Id));
        }

        public Task<Result<Guid>> CommitAsync(string teamId, Guid operationId, ActorIdentity actor, CancellationToken ct)
        {
            Calls.Add("commit:" + teamId);
            Actor = actor;
            return Task.FromResult(Result<Guid>.Success(operationId));
        }

        public Task<Result<DemoReseedDto>> GetAsync(string teamId, Guid operationId, ActorIdentity actor, CancellationToken ct)
        {
            Calls.Add("get:" + teamId);
            Actor = actor;
            return Task.FromResult(Result<DemoReseedDto>.Success(new(operationId, teamId, Guid.NewGuid(), "AwaitingApprovalOrApply", [teamId], "test")));
        }

        public Task<Result<Guid>> AbortAsync(string teamId, Guid operationId, ActorIdentity actor, CancellationToken ct)
        {
            Calls.Add("abort:" + teamId);
            return Task.FromResult(Result<Guid>.Success(operationId));
        }

        public Task<Result> OptInAsync(DemoResetScopeManifest manifest, ActorIdentity actor, CancellationToken ct) =>
            Task.FromResult(Result.Success());

        public Task<Result> ClearAsync(Guid tenantId, CancellationToken ct)
        {
            Calls.Add("clear");
            return Task.FromResult(Result.Success());
        }

        public Task<Result<string>> LocateTeamAsync(Guid operationId, CancellationToken ct) =>
            Task.FromResult(Result<string>.Success("demo"));

    }
}
