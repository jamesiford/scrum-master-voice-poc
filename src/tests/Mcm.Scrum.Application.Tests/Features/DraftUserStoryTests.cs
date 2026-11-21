using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Application.Features.Stories.DraftUserStory;
using Mcm.Scrum.Application.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;

namespace Mcm.Scrum.Application.Tests.Features;

public sealed class DraftUserStoryTests
{
    [Fact]
    public async Task Handle_NoModelReply_ReturnsTemplateDraft()
    {
        await using var h = await StandupHarness.CreateAsync(configure: s => s.AddSingleton<IDraftingAgent>(new FakeDraftingAgent(null)));

        var result = await h.Get<ISender>().Send(new DraftUserStoryQuery("t1", "retry failed payments"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.FromModel);
        Assert.NotEmpty(result.Value.AcceptanceCriteria);
    }

    [Fact]
    public async Task Handle_ModelReply_UsesTheModelDraft()
    {
        const string reply = """{"title":"Retry payments","story":"As a customer, I want retries, so that I pay.","acceptanceCriteria":["Retries three times"]}""";
        await using var h = await StandupHarness.CreateAsync(configure: s => s.AddSingleton<IDraftingAgent>(new FakeDraftingAgent(reply)));

        var result = await h.Get<ISender>().Send(new DraftUserStoryQuery("t1", "retry failed payments"), CancellationToken.None);

        Assert.True(result.Value.FromModel);
        Assert.Equal("Retry payments", result.Value.Title);
    }

    [Fact]
    public async Task Handle_EmptyTopic_IsInvalid()
    {
        await using var h = await StandupHarness.CreateAsync(configure: s => s.AddSingleton<IDraftingAgent>(new FakeDraftingAgent(null)));

        var result = await h.Get<ISender>().Send(new DraftUserStoryQuery("t1", " "), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}
