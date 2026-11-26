using Zava.Scrum.Domain.Members;

namespace Zava.Scrum.Domain.Tests.Members;

public sealed class ParticipantAttributionTests
{
    private static readonly Guid ObjectId = Guid.NewGuid();
    private static readonly Member Member = new("daniel", "team-one", "daniel", "Daniel", null, "", 1, true, EntraObjectId: ObjectId);

    [Fact]
    public void Resolve_VerifiedObjectId_ReturnsMember() =>
        Assert.Equal(Member, ParticipantAttribution.Resolve("team-one", ObjectId, [Member]));

    [Fact]
    public void Resolve_SameNameButDifferentIdentity_RemainsGuest() =>
        Assert.Null(ParticipantAttribution.Resolve("team-one", Guid.NewGuid(), [Member]));

    [Fact]
    public void Resolve_CrossTeamOrAnonymous_RemainsGuest()
    {
        Assert.Null(ParticipantAttribution.Resolve("team-two", ObjectId, [Member]));
        Assert.Null(ParticipantAttribution.Resolve("team-one", null, [Member]));
    }

    [Fact]
    public void Resolve_AmbiguousIdentity_RemainsGuest() =>
        Assert.Null(ParticipantAttribution.Resolve("team-one", ObjectId, [Member, Member with { Id = "duplicate" }]));
}
