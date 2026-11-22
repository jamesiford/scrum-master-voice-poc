using System.Reflection;
using Zava.Scrum.Application.Abstractions.Messaging;
using NetArchTest.Rules;

namespace Zava.Scrum.ArchitectureTests;

/// <summary>Enforces the Clean Architecture dependency rule and CQRS naming conventions (SPEC 6.1, 19).</summary>
public sealed class DependencyRuleTests
{
    private const string Domain = "Zava.Scrum.Domain";
    private const string Application = "Zava.Scrum.Application";
    private const string Infrastructure = "Zava.Scrum.Infrastructure";
    private const string Api = "Zava.Scrum.Api";

    private static readonly Assembly DomainAssembly = typeof(Zava.Scrum.Domain.Common.Result).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(IRequest<>).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Zava.Scrum.Infrastructure.SystemClock).Assembly;

    [Fact]
    public void Domain_HasNoDependencyOnOtherLayersOrFrameworks()
    {
        var forbidden = new[]
        {
            Application, Infrastructure, Api, "Microsoft.AspNetCore", "Microsoft.Extensions", "System.Net.Http",
            "System.Text.Json", "Azure", "FluentValidation",
        };

        var result = Types.InAssembly(DomainAssembly).ShouldNot().HaveDependencyOnAny(forbidden).GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot().HaveDependencyOnAny(Infrastructure, Api, "Microsoft.AspNetCore", "Azure")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        var result = Types.InAssembly(InfrastructureAssembly).ShouldNot().HaveDependencyOn(Api).GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Handlers_AreInternalSealedAndNamedHandler()
    {
        var handlers = Types.InAssembly(ApplicationAssembly)
            .That().ImplementInterface(typeof(IRequestHandler<,>));

        var result = handlers.Should().NotBePublic().And().BeSealed().And().HaveNameEndingWith("Handler").GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Handlers_DoNotDependOnOtherHandlers()
    {
        var offenders = ApplicationAssembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(IsRequestHandler))
            .Where(t => t.GetConstructors().SelectMany(c => c.GetParameters())
                .Any(p => p.ParameterType.GetInterfaces().Any(IsRequestHandler) || IsRequestHandler(p.ParameterType)))
            .Select(t => t.Name)
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void CommandsAndQueries_AreSealedAndNamedAccordingly()
    {
        var requests = ApplicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(i => IsGeneric(i, typeof(IRequest<>))))
            .ToArray();

        var offenders = requests
            .Where(t => !t.IsSealed || !(t.Name.EndsWith("Command", StringComparison.Ordinal) || t.Name.EndsWith("Query", StringComparison.Ordinal)))
            .Select(t => t.Name)
            .ToArray();

        Assert.NotEmpty(requests);
        Assert.Empty(offenders);
    }

    [Fact]
    public void Namespaces_MatchFolderLayout_NoDumpingGrounds()
    {
        var banned = new[] { "Helper", "Util", "Manager", "Common" };
        var offenders = ApplicationAssembly.GetTypes()
            .Where(t => t.Namespace is not null && banned.Any(b => t.Namespace.Split('.').Any(s => s.Contains(b, StringComparison.Ordinal))))
            .Select(t => t.FullName)
            .ToArray();

        Assert.Empty(offenders);
    }

    private static bool IsRequestHandler(Type type) => IsGeneric(type, typeof(IRequestHandler<,>));

    private static bool IsGeneric(Type type, Type definition) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == definition;

    private static string Describe(TestResult result) =>
        result.FailingTypeNames is null ? string.Empty : "Failing types: " + string.Join(", ", result.FailingTypeNames);
}
