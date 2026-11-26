using System.Reflection;
using NetArchTest.Rules;
using Zava.Scrum.Application.Abstractions.Ports;

namespace Zava.Scrum.ArchitectureTests;

/// <summary>The structural rules behind delegated Azure DevOps writes (ADR 0020).</summary>
public sealed class DelegatedWriteRulesTests
{
    private static readonly Assembly InfrastructureAssembly = typeof(Zava.Scrum.Infrastructure.SystemClock).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Zava.Scrum.Api.Auth.UserAssertionMiddleware).Assembly;

    [Fact]
    public void WorkItemWriter_EveryMethodTakesTheDelegatedWriteContext_SoNoWriteExistsWithoutAnApprover()
    {
        var methods = typeof(IWorkItemWriter).GetMethods();

        Assert.NotEmpty(methods);
        Assert.All(methods, m => Assert.Contains(m.GetParameters(), p => p.ParameterType == typeof(DelegatedWriteContext)));
        Assert.Single(methods);
    }

    [Fact]
    public void AzureDevOpsClients_AreNotPublic_SoOnlyTheInfrastructureCompositionBuildsOne()
    {
        var names = new[] { "AdoClientFactory", "IAdoClientFactory", "AdoRestClient", "AdoWorkItemWriter", "RoutedWorkItemWriter", "RoutedWorkItemReader" };
        var exposed = InfrastructureAssembly.GetTypes().Where(t => names.Contains(t.Name) && (t.IsPublic || t.IsNestedPublic)).Select(t => t.FullName).ToList();

        Assert.Empty(exposed);
    }

    [Fact]
    public void Api_DoesNotUseTheAzureDevOpsClientsOrTheirCredentials()
    {
        var result = Types.InAssembly(ApiAssembly).ShouldNot().HaveDependencyOnAny("Zava.Scrum.Infrastructure.Ado.AdoClientFactory", "Zava.Scrum.Infrastructure.Ado.AdoRestClient", "Zava.Scrum.Infrastructure.Ado.AdoWorkItemWriter", "Zava.Scrum.Infrastructure.Http.BearerTokenHandler", "Zava.Scrum.Infrastructure.Http.PatAuthHandler").GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void AgentHost_WhenItExists_ReferencesNoAzureDevOpsCredentials()
    {
        var root = FindRepositoryRoot();
        var host = Path.Combine(root, "src", "Zava.Scrum.AgentHost");
        if (!Directory.Exists(host))
        {
            return;
        }

        var forbidden = new[] { "AdoOptions", "AdoClientFactory", "499b84ac-1321-427f-aa17-267ca6975798", "Ado:Pat", "PatAuthHandler", "OboAdoTokenSource", "IApproverTokenSource" };
        var hits = Directory.EnumerateFiles(host, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".cs", StringComparison.Ordinal) || f.EndsWith(".json", StringComparison.Ordinal) || f.EndsWith(".csproj", StringComparison.Ordinal))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f => forbidden.Any(token => File.ReadAllText(f).Contains(token, StringComparison.Ordinal)))
            .ToList();

        Assert.Empty(hits);
    }

    [Fact]
    public void AgentHost_WhenItExists_HoldsNoSecretOfItsOwn()
    {
        var host = Path.Combine(FindRepositoryRoot(), "src", "Zava.Scrum.AgentHost");
        if (!Directory.Exists(host))
        {
            return;
        }

        // The hosted agent signs in as its own Entra Agent ID (ADR 0022): no client secret, key or connection string in its source or settings.
        var forbidden = new[] { "ClientSecret", "client_secret", "AccountKey", "ConnectionString", "SharedAccessKey", "ApiKey", "api-key" };
        var hits = Directory.EnumerateFiles(host, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".cs", StringComparison.Ordinal) || f.EndsWith(".json", StringComparison.Ordinal))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f => forbidden.Any(token => File.ReadAllText(f).Contains(token, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.Empty(hits);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ZavaScrum.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("ZavaScrum.slnx not found above the test output.");
    }
}
