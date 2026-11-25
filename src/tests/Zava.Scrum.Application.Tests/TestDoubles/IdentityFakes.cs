using Microsoft.Extensions.DependencyInjection;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Identity;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Identity;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Tests.TestDoubles;

/// <summary>An in-memory append-only approval trail. Offers no update or delete, like the port.</summary>
internal sealed class InMemoryApprovalRecordStore : IApprovalRecordStore
{
    private readonly List<ApprovalRecord> _records = [];

    public IReadOnlyList<ApprovalRecord> All => _records;

    public Task AppendAsync(ApprovalRecord record, CancellationToken ct)
    {
        _records.Add(record);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ApprovalRecord>> ListByFollowUpAsync(Guid followUpId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ApprovalRecord>>(_records.Where(r => r.FollowUpId == followUpId).ToList());

    public Task<IReadOnlyList<ApprovalRecord>> ListByCorrelationAsync(string correlationPrefix, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ApprovalRecord>>(_records.Where(r => r.CorrelationId.StartsWith(correlationPrefix, StringComparison.Ordinal)).ToList());
}

/// <summary>A fixed tenant and administrator list.</summary>
internal sealed class FakeIdentityPolicy(Guid tenantId, params Guid[] admins) : IIdentityPolicy
{
    public Guid TenantId { get; } = tenantId;

    public bool IsAdmin(Guid objectId) => admins.Contains(objectId);
}

/// <summary>Resolves user principal names from a fixed table.</summary>
internal sealed class FakeIdentityDirectory(IReadOnlyDictionary<string, Guid>? users = null) : IIdentityDirectory
{
    public Task<Guid?> FindObjectIdAsync(string userPrincipalName, CancellationToken ct) =>
        Task.FromResult(users is not null && users.TryGetValue(userPrincipalName, out var id) ? (Guid?)id : null);
}

/// <summary>An in-memory membership store.</summary>
internal sealed class InMemoryMembershipRepository : ITeamMembershipRepository
{
    private readonly List<TeamMembership> _items = [];

    public Task<IReadOnlyList<TeamMembership>> ListByTeamAsync(string teamId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<TeamMembership>>(_items.Where(m => m.TeamId == teamId).ToList());

    public Task<IReadOnlyList<TeamMembership>> ListByMemberAsync(Guid objectId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<TeamMembership>>(_items.Where(m => m.ObjectId == objectId).ToList());

    public Task UpsertAsync(TeamMembership membership, CancellationToken ct)
    {
        _items.RemoveAll(m => m.TeamId == membership.TeamId && m.ObjectId == membership.ObjectId);
        _items.Add(membership);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string teamId, Guid objectId, CancellationToken ct)
    {
        _items.RemoveAll(m => m.TeamId == teamId && m.ObjectId == objectId);
        return Task.CompletedTask;
    }
}

/// <summary>Reports each work item as living in the project and area given, or in the team's own area when none is set for it.</summary>
internal sealed class FakeScopeReader : IWorkItemScopeReader
{
    private readonly Dictionary<int, WorkItemLocation> _locations = [];

    public void Place(int id, string project, string areaPath) => _locations[id] = new WorkItemLocation(project, areaPath);

    public Task<Result<WorkItemLocation>> GetLocationAsync(AdoSettings ado, int workItemId, CancellationToken ct) =>
        Task.FromResult(Result<WorkItemLocation>.Success(_locations.GetValueOrDefault(workItemId) ?? new WorkItemLocation(ado.Project, ado.AreaPath)));
}

/// <summary>Well-known identities and decisions for tests.</summary>
internal static class Who
{
    public static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid AdminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    /// <summary>A Teams administrator (a configured object id).</summary>
    public static DecisionContext TeamsAdmin(string name = "Dana") => DecisionContext.FromTeams(Actor(AdminId, name, ActorSource.Teams));

    /// <summary>A Teams user with the given object id.</summary>
    public static DecisionContext TeamsUser(Guid objectId, string name = "Priya") => DecisionContext.FromTeams(Actor(objectId, name, ActorSource.Teams));

    /// <summary>A portal caller holding the given roles.</summary>
    public static DecisionContext Portal(string name = "Dana", params string[] roles) =>
        new(Actor(AdminId, name, ActorSource.Portal), ApprovalChannel.Portal, AuthenticationContext.Of("entra-bearer"), roles.Length == 0 ? [AppRoles.Approver] : roles);

    private static readonly AsyncLocal<UserAssertion?> SignedIn = new();

    /// <summary>The token the test caller presents: whoever the most recent test identity was, because a real caller signs in as themselves.</summary>
    public static UserAssertion? Signed { get => SignedIn.Value; set => SignedIn.Value = value; }

    public static ActorIdentity Actor(Guid objectId, string name, ActorSource source)
    {
        Signed = new UserAssertion($"token-of-{objectId}", objectId, Tenant);
        return ActorIdentity.Create(objectId.ToString(), Tenant.ToString(), name, null, source, source == ActorSource.Portal ? "entra-bearer" : "bot-service-token").Value;
    }

    /// <summary>Registers the identity ports with the test tenant and <see cref="AdminId"/> as administrator.</summary>
    public static IServiceCollection AddIdentityFakes(this IServiceCollection services, InMemoryApprovalRecordStore? records = null, IReadOnlyDictionary<string, Guid>? users = null)
    {
        services.AddSingleton<IIdentityPolicy>(new FakeIdentityPolicy(Tenant, AdminId));
        services.AddSingleton<IIdentityDirectory>(new FakeIdentityDirectory(users));
        services.AddSingleton<IApprovalRecordStore>(records ?? new InMemoryApprovalRecordStore());
        services.AddSingleton<ITeamMembershipRepository>(new InMemoryMembershipRepository());
        services.AddSingleton<IWorkItemScopeReader>(new FakeScopeReader());
        services.AddSingleton<IUserAssertionAccessor, AmbientAssertionAccessor>();
        services.AddSingleton<IApproverTokenSource>(new FakeApproverTokenSource());
        var teams = new InMemoryTeamRepository();
        teams.SaveAsync(new Team("t1", "Payments", "UTC", 15, new AdoSettings("o", "p", "t", "a"), new TeamBehaviours(), "Ship retries"), default).GetAwaiter().GetResult();
        services.AddSingleton<ITeamRepository>(teams);
        return services;
    }
}

/// <summary>Holds the token the current test caller presents, so a test sets it by choosing who it acts as (<see cref="Who.Signed"/>).</summary>
internal sealed class AmbientAssertionAccessor : IUserAssertionAccessor
{
    public UserAssertion? Current { get => Who.Signed; set => Who.Signed = value; }
}

/// <summary>Exchanges any assertion for a token that names its owner, or refuses when <see cref="Refuse"/> is set.</summary>
internal sealed class FakeApproverTokenSource : IApproverTokenSource
{
    public bool Refuse { get; set; }

    public Task<Result<DelegatedAdoAccess>> ExchangeAsync(UserAssertion assertion, CancellationToken ct) =>
        Task.FromResult(Refuse
            ? Result<DelegatedAdoAccess>.Failure(Error.ExternalRejected("delegation.exchange_failed", "no consent"))
            : Result<DelegatedAdoAccess>.Success(new DelegatedAdoAccess($"ado-for-{assertion.ObjectId}")));
}
