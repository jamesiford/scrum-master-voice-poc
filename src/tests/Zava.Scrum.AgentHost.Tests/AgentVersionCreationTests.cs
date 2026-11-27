using System.ClientModel;
using Zava.Scrum.Tools.AgentDeploy;

namespace Zava.Scrum.AgentHost.Tests;

/// <summary>Verifies the bounded, service-directed recovery used during greenfield version creation.</summary>
public sealed class AgentVersionCreationTests
{
    private const string WorkspaceConflict = "The project workspace identity changed during version creation. Read the current agent and retry.";

    [Fact]
    public void Create_WorkspaceConflict_ReadsCurrentAgentBeforeRetry()
    {
        // Arrange
        var operations = new List<string>();
        var creations = 0;
        int CreateVersion()
        {
            operations.Add("create");
            return ++creations == 1 ? throw new ServiceFailure(409, WorkspaceConflict) : 42;
        }

        // Act
        var version = AgentVersionCreation.Create(CreateVersion, () => operations.Add("read"));

        // Assert
        Assert.Equal(42, version);
        Assert.Equal(["create", "read", "create"], operations);
    }

    [Fact]
    public void Create_PersistentWorkspaceConflict_StopsAfterTwoRetries()
    {
        // Arrange
        var failure = new ServiceFailure(409, WorkspaceConflict);
        var creations = 0;
        var reads = 0;
        int CreateVersion()
        {
            creations++;
            throw failure;
        }

        // Act
        var actual = Assert.Throws<ServiceFailure>(() => AgentVersionCreation.Create(CreateVersion, () => reads++));

        // Assert
        Assert.Same(failure, actual);
        Assert.Equal(3, creations);
        Assert.Equal(2, reads);
    }

    [Theory]
    [InlineData(403, WorkspaceConflict)]
    [InlineData(429, WorkspaceConflict)]
    [InlineData(409, "Another deployment owns this resource.")]
    public void Create_AuthenticationOrUnrelatedFailure_DoesNotRetry(int status, string message)
    {
        // Arrange
        var failure = new ServiceFailure(status, message);
        var creations = 0;
        var reads = 0;
        int CreateVersion()
        {
            creations++;
            throw failure;
        }

        // Act
        var actual = Assert.Throws<ServiceFailure>(() => AgentVersionCreation.Create(CreateVersion, () => reads++));

        // Assert
        Assert.Same(failure, actual);
        Assert.Equal(1, creations);
        Assert.Equal(0, reads);
    }

    [Fact]
    public void Create_CurrentAgentReadRefused_DoesNotRetryCreation()
    {
        // Arrange
        var failure = new ServiceFailure(403, "Current agent read refused.");
        var creations = 0;
        var reads = 0;
        int CreateVersion()
        {
            creations++;
            throw new ServiceFailure(409, WorkspaceConflict);
        }
        void ReadCurrentAgent()
        {
            reads++;
            throw failure;
        }

        // Act
        var actual = Assert.Throws<ServiceFailure>(() => AgentVersionCreation.Create(CreateVersion, ReadCurrentAgent));

        // Assert
        Assert.Same(failure, actual);
        Assert.Equal(1, creations);
        Assert.Equal(1, reads);
    }

    private sealed class ServiceFailure : ClientResultException
    {
        internal ServiceFailure(int status, string message) : base(message) => Status = status;
    }
}
