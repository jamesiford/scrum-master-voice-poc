using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Api.Tests;

/// <summary>In-app bearer validation on every /api route (ADR 0017) and the team-scoped authorisation matrix (ADR 0018).</summary>
public sealed class PortalAuthorizationTests : IClassFixture<ApiFactory>
{
    private static readonly Guid LeadA = Guid.Parse("a0000000-0000-0000-0000-00000000000a");
    private static readonly Guid ReaderA = Guid.Parse("a0000000-0000-0000-0000-00000000000b");
    private static readonly Guid LeadB = Guid.Parse("b0000000-0000-0000-0000-00000000000a");
    private static readonly Guid Outsider = Guid.Parse("c0000000-0000-0000-0000-00000000000c");
    private static readonly Guid SessionB = Guid.Parse("b0000000-0000-0000-0000-0000000000b1");
    private static readonly Guid FollowUpB = Guid.Parse("b0000000-0000-0000-0000-0000000000f1");

    private readonly ApiFactory _factory;

    public PortalAuthorizationTests(ApiFactory factory)
    {
        _factory = factory;
        Seed().GetAwaiter().GetResult();
    }

    public static TheoryData<string, string> BadTokens() => new()
    {
        { "another tenant", "tenant" },
        { "wrong audience", "audience" },
        { "wrong issuer", "issuer" },
        { "no oid", "oid" },
        { "no tid", "tid" },
        { "expired", "expired" },
        { "signed with another key", "key" },
    };

    // Every route family that carries a team, aimed at team B (by route, query, body, or the team of the session or follow-up it names).
    public static TheoryData<string, string> TeamBRoutes() => new()
    {
        { "GET", "/api/teams/team-b" },
        { "GET", "/api/teams/team-b/memberships" },
        { "GET", "/api/teams/team-b/members" },
        { "GET", "/api/teams/team-b/schedule" },
        { "PUT", "/api/teams/team-b/schedule" },
        { "GET", "/api/teams/team-b/prompts" },
        { "GET", "/api/teams/team-b/prompts/facilitator" },
        { "PUT", "/api/teams/team-b/settings" },
        { "POST", "/api/teams/team-b/members/sync" },
        { "GET", "/api/sessions?teamId=team-b" },
        { "GET", $"/api/sessions/{SessionB}" },
        { "GET", $"/api/sessions/{SessionB}/events" },
        { "POST", $"/api/sessions/{SessionB}/control" },
        { "POST", $"/api/sessions/{SessionB}/start" },
        { "POST", "/api/sessions/prepare" },
        { "GET", "/api/followups?teamId=team-b" },
        { "GET", $"/api/followups/{FollowUpB}" },
        { "POST", $"/api/followups/{FollowUpB}/approve" },
        { "POST", $"/api/followups/{FollowUpB}/reject" },
        { "POST", $"/api/followups/{FollowUpB}/apply" },
        { "GET", "/api/cards?teamId=team-b" },
        { "GET", "/api/planning?teamId=team-b" },
        { "POST", "/api/planning/prepare" },
        { "GET", "/api/calendar/proposals?teamId=team-b" },
        { "GET", "/api/retrospective?teamId=team-b" },
        { "GET", "/api/reports/kpis?teamId=team-b" },
        { "GET", "/api/reports/sprint?teamId=team-b" },
        { "GET", "/api/stories/topics?teamId=team-b" },
        { "GET", "/api/models?teamId=team-b" },
        { "GET", "/api/demo/status?teamId=team-b" },
        { "POST", "/api/ask" },
        { "POST", "/api/retrospective/prepare" },
        { "POST", "/api/stories/draft" },
        { "POST", "/api/calendar/propose" },
        { "POST", "/api/ceremonies/Retrospective/pack" },
    };

    [Fact]
    public async Task Api_NoToken_Is401()
    {
        using var client = _factory.ClientWith(null);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/health")).StatusCode);
    }

    [Fact]
    public async Task Api_EasyAuthHeadersAreNotTrusted()
    {
        using var client = _factory.ClientWith(null);
        var principal = Convert.ToBase64String("""{"auth_typ":"aad","claims":[{"typ":"roles","val":"Zava.Scrum.Admin"},{"typ":"oid","val":"22222222-2222-2222-2222-222222222222"}]}"""u8.ToArray());
        client.DefaultRequestHeaders.Add("X-MS-CLIENT-PRINCIPAL", principal);
        client.DefaultRequestHeaders.Add("X-MS-CLIENT-PRINCIPAL-ID", "22222222-2222-2222-2222-222222222222");

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/health")).StatusCode);
    }

    [Fact]
    public async Task Api_AnyValidToken_CanReadHealth_BecauseHealthNamesNoTeam()
    {
        using var client = _factory.ClientWith(_factory.Token());

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/health")).StatusCode);
    }

    [Theory]
    [MemberData(nameof(BadTokens))]
    public async Task Api_TokenThatFailsAValidationRule_Is401(string _, string rule)
    {
        var token = rule switch
        {
            "tenant" => _factory.Token(Roles.Admin, tenant: "33333333-3333-3333-3333-333333333333"),
            "audience" => _factory.Token(Roles.Admin, audience: "api://someone-else"),
            "issuer" => _factory.Token(Roles.Admin, issuer: "https://login.microsoftonline.com/33333333-3333-3333-3333-333333333333/v2.0"),
            "oid" => _factory.Token(Roles.Admin, oid: null),
            "tid" => _factory.Token(Roles.Admin, tenant: null),
            "expired" => _factory.Token(Roles.Admin, expires: DateTime.UtcNow.AddHours(-2)),
            _ => _factory.Token(Roles.Admin, signingKey: ApiFactory.WrongKey()),
        };
        using var client = _factory.ClientWith(token);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/health")).StatusCode);
    }

    [Fact]
    public async Task Api_AudienceMayBeTheApiUriForm()
    {
        using var client = _factory.ClientWith(_factory.Token(audience: $"api://{ApiFactory.ClientId}"));

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/health")).StatusCode);
    }

    [Theory]
    [MemberData(nameof(TeamBRoutes))]
    public async Task TeamBRoute_ACallerWhoIsOnlyInTeamA_IsAnsweredNotFound_AsIfTheTeamDidNotExist(string method, string url)
    {
        using var client = _factory.ClientWith(_factory.Token(oid: LeadA.ToString()));

        var response = await client.SendAsync(Request(method, url));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TeamBRoutes))]
    public async Task TeamBRoute_ACallerWithNoTeamAtAll_IsAnsweredNotFound(string method, string url)
    {
        using var client = _factory.ClientWith(_factory.Token(oid: Outsider.ToString()));

        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(Request(method, url))).StatusCode);
    }

    [Theory]
    [MemberData(nameof(TeamBRoutes))]
    public async Task TeamBRoute_AnAdministrator_IsNeverRefusedByTheTeamCheck(string method, string url)
    {
        using var client = _factory.ClientWith(_factory.Token(Roles.Admin, oid: Outsider.ToString()));

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(Request(method, url));
        }
        catch (Exception)
        {
            // The team check passed and the handler then failed on something outside the test host (a real Azure DevOps call).
            return;
        }

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("team.not_found", body, StringComparison.Ordinal);
        Assert.DoesNotContain("team.forbidden", body, StringComparison.Ordinal);
        Assert.DoesNotContain("team.none", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TeamAData_ATeamLeadReadsItsOwnTeam_AndAReaderCannotChangeIt()
    {
        using var lead = _factory.ClientWith(_factory.Token(oid: LeadA.ToString()));
        using var reader = _factory.ClientWith(_factory.Token(oid: ReaderA.ToString()));

        Assert.Equal(HttpStatusCode.OK, (await lead.GetAsync("/api/sessions?teamId=team-a")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync("/api/sessions?teamId=team-a")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.SendAsync(Request("POST", "/api/sessions/prepare", "team-a"))).StatusCode);
        Assert.DoesNotContain("team.", await (await lead.SendAsync(Request("POST", "/api/sessions/prepare", "team-a"))).Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Approve_ATeamLeadOfAnotherTeamIsRefused_AndTheOwnTeamLeadReachesTheHandler()
    {
        using var other = _factory.ClientWith(_factory.Token(oid: LeadA.ToString()));
        using var lead = _factory.ClientWith(_factory.Token(oid: LeadB.ToString()));

        var otherResponse = await other.PostAsJsonAsync($"/api/followups/{FollowUpB}/approve", new { });
        var leadResponse = await lead.PostAsJsonAsync($"/api/followups/{FollowUpB}/approve", new { });

        Assert.Equal(HttpStatusCode.NotFound, otherResponse.StatusCode);
        Assert.DoesNotContain("team.", await leadResponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Registry_OnlyAnAdministratorMayCreateOrEditTeams()
    {
        using var lead = _factory.ClientWith(_factory.Token(oid: LeadA.ToString()));
        using var admin = _factory.ClientWith(_factory.Token(Roles.Admin));
        var body = new { id = "team-new", name = "New" };

        Assert.Equal(HttpStatusCode.Forbidden, (await lead.PostAsJsonAsync("/api/teams", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await lead.GetAsync("/api/fleet")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await lead.PostAsync("/api/demo/reset", null)).StatusCode);
        Assert.DoesNotContain("team.forbidden", await (await admin.PostAsJsonAsync("/api/teams", body)).Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task TeamList_ShowsACallerOnlyTheirOwnTeams_AndAnAdministratorEveryTeam()
    {
        using var lead = _factory.ClientWith(_factory.Token(oid: LeadA.ToString()));
        using var admin = _factory.ClientWith(_factory.Token(Roles.Admin, oid: Outsider.ToString()));

        var mine = await Ids(lead);
        var all = await Ids(admin);

        Assert.Equal(["team-a"], mine);
        Assert.Contains("team-a", all);
        Assert.Contains("team-b", all);
    }

    [Fact]
    public async Task Request_ThatNamesNoTeam_IsRefusedNotAnswered()
    {
        using var lead = _factory.ClientWith(_factory.Token(oid: LeadA.ToString()));

        var response = await lead.PostAsJsonAsync("/api/sessions/prepare", new { });

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Audit_NeedsASignedInCaller_AndAnUnknownCorrelationIsNotFound()
    {
        var path = "/api/audit/4bf92f3577b34da6a3ce929d0e0e4736";
        using var none = _factory.ClientWith(null);
        using var admin = _factory.ClientWith(_factory.Token(Roles.Admin));

        Assert.Equal(HttpStatusCode.Unauthorized, (await none.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task OutsideApi_HealthAndPortalConfigAreAnonymous_AndTheBridgeKeepsItsOwnToken()
    {
        using var client = _factory.ClientWith(null);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/healthz")).StatusCode);
        var config = await client.GetFromJsonAsync<PortalConfig>("/portal-config.json");
        Assert.Equal(ApiFactory.ClientId, config!.ClientId);
        Assert.Equal(ApiFactory.TenantId, config.TenantId);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/ws/bridge/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public void EveryApiRoute_NeedsASignIn_AndIsClassified_AsTeamScopedRegistryOnlyOrTeamFree()
    {
        var routes = _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText?.StartsWith("/api/", StringComparison.Ordinal) == true && e.RoutePattern.RawText != "/api/messages")
            .ToList();
        Assert.NotEmpty(routes);

        foreach (var route in routes)
        {
            var text = $"{string.Join(',', route.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods)} {route.RoutePattern.RawText}";
            Assert.True(route.Metadata.GetOrderedMetadata<IAuthorizeData>().Any(), $"{text} needs a sign-in");
            var kinds = route.Metadata.Select(m => m.GetType().Name).ToList();
            Assert.True(
                kinds.Contains("TeamPermissionMetadata") || kinds.Contains("RegistryOnlyMetadata") || kinds.Contains("NoTeamScopeMetadata"),
                $"{text} is not classified for team authorisation");
        }

        var decisions = routes.Where(r => r.RoutePattern.RawText!.EndsWith("/approve", StringComparison.Ordinal) || r.RoutePattern.RawText.EndsWith("/reject", StringComparison.Ordinal));
        foreach (var route in decisions)
        {
            Assert.Contains(route.Metadata, m => m.GetType().Name == "TeamPermissionMetadata" && m.ToString()!.Contains("Approve", StringComparison.Ordinal));
        }
    }

    private static HttpRequestMessage Request(string method, string url, string teamId = "team-b") =>
        new(new HttpMethod(method), url) { Content = method == "GET" ? null : JsonContent.Create(new { teamId, sessionId = SessionB, question = "q", mode = "Simulated", action = "pause", topic = "t", kind = "Retrospective" }) };

    private static async Task<List<string>> Ids(HttpClient client)
    {
        using var doc = JsonDocument.Parse(await client.GetStringAsync("/api/teams"));
        return [.. doc.RootElement.EnumerateArray().Select(e => e.GetProperty("id").GetString()!).Order(StringComparer.Ordinal)];
    }

    private async Task Seed()
    {
        var sp = _factory.Services;
        var teams = sp.GetRequiredService<ITeamRepository>();
        foreach (var id in new[] { "team-a", "team-b" })
        {
            await teams.SaveAsync(new Team(id, id, "UTC", 15, new AdoSettings("https://dev.azure.com/zava", id, id, id), new TeamBehaviours(), "goal"), default);
        }

        var members = sp.GetRequiredService<ITeamMembershipRepository>();
        await members.UpsertAsync(new TeamMembership("team-a", LeadA, TeamRole.TeamLead), default);
        await members.UpsertAsync(new TeamMembership("team-a", ReaderA, TeamRole.Reader), default);
        await members.UpsertAsync(new TeamMembership("team-b", LeadB, TeamRole.TeamLead), default);

        var now = new DateTimeOffset(2026, 1, 8, 9, 0, 0, TimeSpan.Zero);
        await sp.GetRequiredService<ISessionRepository>().SaveAsync(StandupSession.Prepare(SessionB, "team-b", now, null), default);
        var followUp = FollowUp.Create(FollowUpB, "team-b", SessionB, null, FollowUpKind.Followup, "m", [new ProposedChange(1, ChangeOp.Comment, "x", "y")], new DateOnly(2026, 1, 8), now);
        await sp.GetRequiredService<IFollowUpRepository>().SaveAsync(followUp, default);
    }

    private sealed record PortalConfig(string? ClientId, string? TenantId);
}
