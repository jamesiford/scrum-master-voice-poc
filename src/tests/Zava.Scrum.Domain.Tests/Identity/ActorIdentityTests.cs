using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Identity;

namespace Zava.Scrum.Domain.Tests.Identity;

public sealed class ActorIdentityTests
{
    private const string Oid = "22222222-2222-2222-2222-222222222222";
    private const string Tid = "11111111-1111-1111-1111-111111111111";

    [Fact]
    public void Create_ValidClaims_ReturnsTheIdentity()
    {
        var result = ActorIdentity.Create(Oid, Tid, " Dana ", " dana@zava.example ", ActorSource.Portal, "entra-bearer");

        Assert.True(result.IsSuccess);
        Assert.Equal(Guid.Parse(Oid), result.Value.ObjectId);
        Assert.Equal("Dana", result.Value.DisplayName);
        Assert.Equal("dana@zava.example", result.Value.UserPrincipalName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Dana")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Create_ADisplayNameOrEmptyIdInPlaceOfAnObjectId_IsRejected(string? objectId)
    {
        var result = ActorIdentity.Create(objectId, Tid, "Dana", null, ActorSource.Teams, "bot-service-token");

        Assert.Equal("identity.object_id_required", result.Error!.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Create_NoTenant_IsRejected(string? tenant)
    {
        Assert.Equal("identity.tenant_required", ActorIdentity.Create(Oid, tenant, "Dana", null, ActorSource.Teams, "x").Error!.Code);
    }

    [Fact]
    public void Create_NoDisplayNameOrMethod_IsRejected()
    {
        Assert.Equal("identity.display_name_required", ActorIdentity.Create(Oid, Tid, " ", null, ActorSource.Teams, "x").Error!.Code);
        Assert.Equal("identity.authentication_method_required", ActorIdentity.Create(Oid, Tid, "Dana", null, ActorSource.Teams, " ").Error!.Code);
    }
}

public sealed class CorrelationTests
{
    [Fact]
    public void New_IsAWellFormedW3cTraceId()
    {
        var id = Correlation.New();

        Assert.True(Correlation.IsValid(id));
        Assert.NotEqual(id, Correlation.New());
    }

    [Fact]
    public void Tag_UsesTheTwelveCharacterShortForm()
    {
        Assert.Equal("zsa-4bf92f3577b3", Correlation.Tag("4bf92f3577b34da6a3ce929d0e0e4736"));
    }

    [Theory]
    [InlineData("zsa-4bf92f3577b3", "4bf92f3577b3")]
    [InlineData("ZSA-4BF92F3577B3", "4bf92f3577b3")]
    [InlineData("4bf92f3577b34da6a3ce929d0e0e4736", "4bf92f3577b34da6a3ce929d0e0e4736")]
    [InlineData("zsa-4bf9", null)]
    [InlineData("zsa-zzzzzzzzzzzz", null)]
    [InlineData("", null)]
    public void ToPrefix_AcceptsTheFullIdOrTheTag(string reference, string? expected)
    {
        Assert.Equal(expected, Correlation.ToPrefix(reference));
    }

    [Fact]
    public void ForLegacySession_IsStableAndValid()
    {
        var session = Guid.NewGuid();

        Assert.Equal(Correlation.ForLegacySession(session), Correlation.ForLegacySession(session));
        Assert.True(Correlation.IsValid(Correlation.ForLegacySession(session)));
    }
}
