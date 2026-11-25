using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Identity;
using Zava.Scrum.Infrastructure.Ado;

namespace Zava.Scrum.Infrastructure.Tests.Ado;

/// <summary>When the approver cannot write, Azure DevOps' refusal becomes a clear failure and nothing is retried or substituted (ADR 0020).</summary>
public sealed class AdoWorkItemWriterRefusalTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 8, 10, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task Apply_WhenAzureDevOpsRefusesTheApprover_FailsAsForbiddenAndSaysNothingWasWritten(HttpStatusCode status)
    {
        var handler = new Refuser(status);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://dev.azure.com/org/") };
        var approver = ActorIdentity.Create(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "Ana", null, ActorSource.Portal, "entra-bearer").Value;
        var followUp = FollowUp.Create(Guid.NewGuid(), "t1", Guid.NewGuid(), null, FollowUpKind.Followup, "m", [new ProposedChange(1, ChangeOp.Comment, "x", "y")], new DateOnly(2026, 1, 8), Now);
        followUp.MarkSent();
        followUp.Approve(approver, Now, ApprovalChannel.Portal);

        var result = await new AdoWorkItemWriter(http, Options.Create(new DemoOptions()), NullLogger<AdoWorkItemWriter>.Instance)
            .ApplyAsync(followUp.GetApprovedChanges().Value[0], CancellationToken.None);

        Assert.Equal("ado.approver_cannot_write", result.Error!.Code);
        Assert.Equal(Zava.Scrum.Domain.Common.ErrorType.Forbidden, result.Error.Type);
        Assert.Contains("Nothing was written", result.Error.Message, StringComparison.Ordinal);
        Assert.Equal(1, handler.Calls);
    }

    private sealed class Refuser(HttpStatusCode status) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent("{}") });
        }
    }
}
