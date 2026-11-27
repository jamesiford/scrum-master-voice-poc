using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Zava.Scrum.Api.Seeding;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Demo.OptInDemoScope;
using Zava.Scrum.Application.Features.Demo.ReseedDemo;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Identity;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Infrastructure.Ado;

namespace Zava.Scrum.Api.Tests;

/// <summary>Real HTTP/Entra middleware and mapped endpoint tests for reset Admin permissions and journal-owned team precedence.</summary>
public sealed class DemoReseedRouteTests : IAsyncLifetime, IAsyncDisposable
{
    private readonly ApiFactory _tokens = new();
    private readonly WebApplicationFactory<Program> _host;
    private readonly Reset _reset = new();
    private readonly string _data = Path.Combine(FindRoot(), ".m11-plan", "reseed-api-test-" + Guid.NewGuid().ToString("N"));

    public DemoReseedRouteTests()
    {
        _host = _tokens.WithWebHostBuilder(builder =>
        {
            // Override the existing fixture's temporary path: this bounded test writes only its own project directory.
            builder.UseSetting("Store:DataPath", _data);
            builder.UseSetting("Demo:PaymentsOnly", "true");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDemoReseed>();
                services.AddSingleton<IDemoReseed>(_reset);
                services.AddSingleton<IDemoTeamRemovalHistory>(_reset);
            });
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = _host.Services.CreateScope();
        var teams = scope.ServiceProvider.GetRequiredService<ITeamRepository>();
        await teams.SaveAsync(Team("reseed-a"), default);
        await teams.SaveAsync(Team("reseed-b"), default);
    }

    [Theory]
    [InlineData("/api/demo/reseed", "POST", "Operate")]
    [InlineData("/api/demo/reseed/{operationId:guid}", "GET", "Read")]
    [InlineData("/api/demo/reseed/{operationId:guid}/commit", "POST", "Operate")]
    [InlineData("/api/demo/reseed/{operationId:guid}/abort", "POST", "Operate")]
    public void Endpoint_ActualMappedMetadata_RequiresAuthenticatedAdminAndTeamPermission(string pattern, string method, string permission)
    {
        var endpoint = Assert.Single(_host.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>(),
            candidate => candidate.RoutePattern.RawText == pattern);
        Assert.Equal([method], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
        Assert.Contains(endpoint.Metadata, metadata => metadata.GetType().Name == "TeamPermissionMetadata"
            && metadata.ToString()!.Contains(permission, StringComparison.Ordinal));
        Assert.DoesNotContain(endpoint.Metadata, metadata => metadata.GetType().Name is "RegistryOnlyMetadata" or "NoTeamScopeMetadata");
        var policy = Assert.Single(endpoint.Metadata.GetOrderedMetadata<AuthorizationPolicy>());
        Assert.Contains(policy.Requirements, requirement => requirement is DenyAnonymousAuthorizationRequirement);
        Assert.Contains(policy.Requirements, requirement => requirement is RolesAuthorizationRequirement roles && roles.AllowedRoles.Contains(AppRoles.Admin));
    }

    [Theory]
    [InlineData("POST", "stage")]
    [InlineData("GET", "status")]
    [InlineData("POST", "commit")]
    [InlineData("POST", "abort")]
    public async Task Endpoint_NoTokenOrReader_RefusesAllFourRoutesBeforeHandler(string method, string action)
    {
        using var anonymous = _host.CreateClient();
        using var reader = Client(Roles.Reader);
        var uri = UriFor(action, "reseed-a");

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.SendAsync(new HttpRequestMessage(new HttpMethod(method), uri))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.SendAsync(new HttpRequestMessage(new HttpMethod(method), uri))).StatusCode);
        Assert.Empty(_reset.Calls);
    }

    [Theory]
    [InlineData("GET", "status")]
    [InlineData("POST", "commit")]
    [InlineData("POST", "abort")]
    public async Task Endpoint_AnotherTeamQueryCannotOverrideOperationOwner_RefusesBeforeHandler(string method, string action)
    {
        using var admin = Client(Roles.Admin);
        var response = await admin.SendAsync(new HttpRequestMessage(new HttpMethod(method), UriFor(action, "reseed-b")));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("demo.operation_team_mismatch", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Empty(_reset.Calls);
    }

    [Fact]
    public async Task StartupSeeder_JournalRemovedConnection_IsNotRecreatedAndOtherSeedingStillWorks()
    {
        _reset.RemovedTeamId = "team-identity";
        var seeder = new DemoTeamSeeder(_host.Services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new DemoOptions
            {
                Teams = [
                new("team-identity", "Identity", "Project", "Identity", "Project\\Identity", "goal", "unused"),
                new("seed-other", "Other", "Project", "Other", "Project\\Other", "goal", "unused")]
            }),
            Options.Create(new AdoOptions { Org = "https://dev.azure.com/test" }), NullLogger<DemoTeamSeeder>.Instance);

        await seeder.StartAsync(default);

        using var scope = _host.Services.CreateScope();
        var teams = scope.ServiceProvider.GetRequiredService<ITeamRepository>();
        Assert.Null(await teams.GetAsync("team-identity", default));
        Assert.NotNull(await teams.GetAsync("seed-other", default));
    }

    [Fact]
    public async Task Stage_UnknownTeamOverride_IsNotFoundBeforeHandler()
    {
        using var admin = Client(Roles.Admin);

        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync(UriFor("stage", "missing-team"), null)).StatusCode);
        Assert.Empty(_reset.Calls);
    }

    public async Task DisposeAsync()
    {
        await _host.DisposeAsync();
        await _tokens.DisposeAsync();
        if (Directory.Exists(_data))
        {
            Directory.Delete(_data, recursive: true);
        }
    }

    ValueTask IAsyncDisposable.DisposeAsync() => new(DisposeAsync());

    private HttpClient Client(string[] roles)
    {
        var client = _host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _tokens.Token(roles));
        return client;
    }

    private string UriFor(string action, string team) => action switch
    {
        "stage" => "/api/demo/reseed?teamId=" + team,
        "status" => $"/api/demo/reseed/{_reset.Id}?teamId={team}",
        _ => $"/api/demo/reseed/{_reset.Id}/{action}?teamId={team}",
    };

    private static Team Team(string id) => new(id, id, "UTC", 15,
        new AdoSettings("https://dev.azure.com/test", "Project", id, "Project\\" + id), new TeamBehaviours(), "test");

    private sealed class Reset : IDemoReseed, IDemoTeamRemovalHistory
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string? RemovedTeamId { get; set; }
        public List<string> Calls { get; } = [];
        public Task<Result<string>> LocateTeamAsync(Guid operationId, CancellationToken ct) =>
            Task.FromResult(operationId == Id ? Result<string>.Success("reseed-a") : Result<string>.Failure(Error.NotFound("demo.operation_not_found", "missing")));
        public Task<Result<Guid>> StageAsync(string teamId, ActorIdentity actor, CancellationToken ct)
        {
            Calls.Add("stage");
            return Task.FromResult(Result<Guid>.Success(Id));
        }
        public Task<Result<Guid>> CommitAsync(string teamId, Guid operationId, ActorIdentity actor, CancellationToken ct)
        {
            Calls.Add("commit");
            return Task.FromResult(Result<Guid>.Success(Id));
        }
        public Task<Result<Guid>> AbortAsync(string teamId, Guid operationId, ActorIdentity actor, CancellationToken ct)
        {
            Calls.Add("abort");
            return Task.FromResult(Result<Guid>.Success(Id));
        }
        public Task<Result<DemoReseedDto>> GetAsync(string teamId, Guid operationId, ActorIdentity actor, CancellationToken ct)
        {
            Calls.Add("get");
            return Task.FromResult(Result<DemoReseedDto>.Success(new(Id, teamId, Guid.NewGuid(), "AwaitingApprovalOrApply", [teamId], "test")));
        }
        public Task<Result> OptInAsync(DemoResetScopeManifest manifest, ActorIdentity actor, CancellationToken ct) => Task.FromResult(Result.Success());
        public Task<Result> ClearAsync(Guid tenantId, CancellationToken ct) => Task.FromResult(Result.Success());
        public Task<bool> IsRemovedTeamAsync(string teamId, CancellationToken ct) => Task.FromResult(teamId == RemovedTeamId);
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ZavaScrum.slnx")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
