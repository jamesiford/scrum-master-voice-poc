using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Identity;
using Zava.Scrum.Infrastructure.Ado;

namespace Zava.Scrum.Infrastructure.Tests.Ado;

/// <summary>Approved per-item seed writes are revision-guarded and safely replayable after an ambiguous response.</summary>
public sealed class DemoReseedWriterTests
{
    [Fact]
    public async Task Apply_ApprovedBaseline_ReplayWritesOnceAndTestsRevision()
    {
        var operation = Guid.NewGuid();
        var transport = new Ado(operation);
        using var http = new HttpClient(transport) { BaseAddress = new Uri("https://dev.azure.com/demo/") };
        var writer = new AdoWorkItemWriter(http, Options.Create(new DemoOptions()), NullLogger<AdoWorkItemWriter>.Instance);
        var followUp = FollowUp.Create(Guid.NewGuid(), "demo", operation, null, FollowUpKind.Followup, "seed",
            [new ProposedChange(7, ChangeOp.ResetDemoItem, "reset", "seed",
                DemoReset: new(operation, "Demo", "Demo\\Payments", "demo-key-one",
                    new Dictionary<string, string?> { [AdoFields.State] = "New", [AdoFields.Tags] = "zava-demo; demo-key-one" }))],
            new DateOnly(2026, 11, 27), DateTimeOffset.UnixEpoch);
        Assert.False(followUp.GetApprovedChanges().IsSuccess);
        followUp.Approve(new ActorIdentity(Guid.NewGuid(), Guid.NewGuid(), "Admin", null, ActorSource.Portal, "entra-bearer"), DateTimeOffset.UnixEpoch);
        var change = followUp.GetApprovedChanges().Value.Single();

        var written = await writer.ApplyAsync(change, default);
        var replay = await writer.ApplyAsync(change, default);

        Assert.True(written.IsSuccess);
        Assert.True(replay.IsSuccess);
        Assert.Equal(1, transport.Writes);
        using var patch = JsonDocument.Parse(transport.Patch!);
        Assert.Equal("test", patch.RootElement[0].GetProperty("op").GetString());
        Assert.Equal("/rev", patch.RootElement[0].GetProperty("path").GetString());
        Assert.Equal("replace", patch.RootElement.EnumerateArray().Single(op => op.GetProperty("path").GetString() == "/fields/System.Tags").GetProperty("op").GetString());
        Assert.Contains("Approved by Admin", transport.Patch!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Apply_ItemLostDemoTag_RefusesBeforePatch()
    {
        var operation = Guid.NewGuid();
        var transport = new Ado(operation) { Tagged = false };
        using var http = new HttpClient(transport) { BaseAddress = new Uri("https://dev.azure.com/demo/") };
        var followup = FollowUp.Create(Guid.NewGuid(), "demo", operation, null, FollowUpKind.Followup, "seed",
            [new ProposedChange(7, ChangeOp.ResetDemoItem, "reset", "seed",
                DemoReset: new(operation, "Demo", "Demo\\Payments", "demo-key-one", new Dictionary<string, string?>()))],
            new DateOnly(2026, 11, 27), DateTimeOffset.UnixEpoch);
        followup.Approve(new ActorIdentity(Guid.NewGuid(), Guid.NewGuid(), "Admin", null, ActorSource.Portal, "entra-bearer"), DateTimeOffset.UnixEpoch);
        var writer = new AdoWorkItemWriter(http, Options.Create(new DemoOptions()), NullLogger<AdoWorkItemWriter>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.ApplyAsync(followup.GetApprovedChanges().Value.Single(), default));

        Assert.Equal(0, transport.Writes);
    }

    private sealed class Ado(Guid operation) : HttpMessageHandler
    {
        public int Writes { get; private set; }
        public bool Tagged { get; init; } = true;
        public string? Patch { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Patch)
            {
                Writes++;
                Patch = await request.Content!.ReadAsStringAsync(cancellationToken);
                return Json("""{"id":7,"rev":6}""");
            }

            var tags = (Tagged ? "zava-demo; " : string.Empty) + "demo-key-one" + (Writes > 0 ? $"; demo-reset-{operation:N}" : string.Empty);
            return Json(JsonSerializer.Serialize(new
            {
                id = 7,
                rev = 5,
                fields = new Dictionary<string, object>
                {
                    ["System.TeamProject"] = "Demo",
                    [AdoFields.AreaPath] = "Demo\\Payments",
                    [AdoFields.Tags] = tags,
                }
            }));
        }

        private static HttpResponseMessage Json(string json) =>
            new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
