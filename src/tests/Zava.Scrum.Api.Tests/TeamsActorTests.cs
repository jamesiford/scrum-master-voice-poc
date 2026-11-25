using Microsoft.Agents.Core.Models;
using Zava.Scrum.Api.Bot;
using Zava.Scrum.Domain.Identity;

namespace Zava.Scrum.Api.Tests;

/// <summary>A Teams actor is the Entra object id and tenant on the activity, never the name.</summary>
public sealed class TeamsActorTests
{
    private const string Oid = "22222222-2222-2222-2222-222222222222";

    private static Activity Message(string? aadObjectId, string? name = "Daniel Park", string? tenant = ApiFactory.TenantId) => new()
    {
        Type = ActivityTypes.Message,
        From = new ChannelAccount { Id = "29:abc", Name = name, AadObjectId = aadObjectId },
        Conversation = new ConversationAccount { Id = "19:x", TenantId = tenant },
    };

    [Fact]
    public void From_ActivityWithObjectIdAndTenant_ReturnsATeamsIdentity()
    {
        var actor = TeamsActor.From(Message(Oid));

        Assert.True(actor.IsSuccess);
        Assert.Equal(Guid.Parse(Oid), actor.Value.ObjectId);
        Assert.Equal(Guid.Parse(ApiFactory.TenantId), actor.Value.TenantId);
        Assert.Equal(ActorSource.Teams, actor.Value.Source);
        Assert.Equal("Daniel Park", actor.Value.DisplayName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Daniel Park")]
    public void From_ADisplayNameWithoutAnObjectId_IsRejected(string? aadObjectId)
    {
        var actor = TeamsActor.From(Message(aadObjectId));

        Assert.Equal("identity.object_id_required", actor.Error!.Code);
    }

    [Fact]
    public void From_NoTenantAnywhere_IsRejected()
    {
        Assert.Equal("identity.tenant_required", TeamsActor.From(Message(Oid, tenant: null)).Error!.Code);
    }

    [Fact]
    public void From_TenantInChannelData_IsUsedWhenTheConversationHasNone()
    {
        var activity = Message(Oid, tenant: null);
        activity.ChannelData = System.Text.Json.JsonDocument.Parse("{\"tenant\":{\"id\":\"" + ApiFactory.TenantId + "\"}}").RootElement.Clone();

        Assert.True(TeamsActor.From(activity).IsSuccess);
    }

    [Fact]
    public void From_NoActivity_IsRejected()
    {
        Assert.False(TeamsActor.From(null).IsSuccess);
    }
}
