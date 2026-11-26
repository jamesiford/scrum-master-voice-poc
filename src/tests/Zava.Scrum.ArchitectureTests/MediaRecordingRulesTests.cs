using NetArchTest.Rules;
using Zava.Scrum.Api.Voice;

namespace Zava.Scrum.ArchitectureTests;

public sealed class MediaRecordingRulesTests
{
    [Fact]
    public void NonRecordingGateway_HasNoPersistenceMessagingOrToolDependencies()
    {
        var forbidden = new[]
        {
                "System.IO.File", "System.IO.FileStream", "System.IO.StreamWriter",
                "Zava.Scrum.Infrastructure.Persistence", "Zava.Scrum.Application.Abstractions.Messaging",
                "Zava.Scrum.Application.Tools.IScrumTools", "Zava.Scrum.Application.Features.Standups.RecordStandupEvent",
                "Zava.Scrum.Application.Features.Standups.ReconcileUpdate", "Zava.Scrum.Application.Features.FollowUps",
                "Zava.Scrum.Application.Abstractions.Ports.ISessionRepository",
                "Zava.Scrum.Application.Abstractions.Ports.IFollowUpRepository",
                "Zava.Scrum.Application.Abstractions.Ports.IWorkItemWriter",
        };
        foreach (var dependency in forbidden)
        {
            var result = Types.InAssembly(typeof(GraphMediaVoiceGateway).Assembly)
                .That().HaveNameStartingWith(nameof(GraphMediaVoiceGateway))
                .ShouldNot().HaveDependencyOn(dependency).GetResult();
            Assert.True(result.IsSuccessful, $"{dependency}: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    [Fact]
    public void MediaHost_SourceHasNoAudioStorageOrWritableApplicationDependencies()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ZavaScrum.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        var host = Path.Combine(directory.FullName, "src", "Zava.Scrum.MediaBot");
        var sources = Directory.EnumerateFiles(host, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(sources);
        var forbidden = new[] { "File.Write", "File.Append", "FileStream", "StreamWriter", "BinaryWriter", "System.IO.File",
            "Azure.Storage", "Azure.Cosmos", "Zava.Scrum.Infrastructure.Persistence", "Zava.Scrum.Application", "redirectToTrace: true" };
        Assert.All(sources, source => Assert.DoesNotContain(forbidden,
            token => File.ReadAllText(source).Contains(token, StringComparison.Ordinal)));
    }
}
