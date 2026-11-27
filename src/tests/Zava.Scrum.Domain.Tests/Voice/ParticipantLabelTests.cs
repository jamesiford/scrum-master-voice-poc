using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Voice;

namespace Zava.Scrum.Domain.Tests.Voice;

/// <summary>Readable participant names use unique object ids, never display-name identity guesses.</summary>
public sealed class ParticipantLabelTests
{
    private static readonly Guid ObjectId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Member RosterMember = new("member", "team", "key", "Dana Admin", null, "Dana", 1, true, EntraObjectId: ObjectId);

    [Fact]
    public void Resolve_UniqueObjectId_UsesRosterNameInsteadOfUntrustedDisplayName()
    {
        var label = ParticipantLabel.Resolve("participant", ObjectId, "Untrusted name", [RosterMember]);

        Assert.Equal("Dana Admin", label.DisplayName);
        Assert.Equal("member", label.MemberId);
        Assert.False(label.Guest);
    }

    [Fact]
    public void Resolve_MatchingNameWithoutIdentity_RemainsGuest()
    {
        var label = ParticipantLabel.Resolve("participant", null, "Dana Admin", [RosterMember]);

        Assert.True(label.Guest);
        Assert.Null(label.MemberId);
    }

    [Fact]
    public void Resolve_DuplicateIdentityOrMissingName_IsExplicitGuest()
    {
        var label = ParticipantLabel.Resolve("participant", ObjectId, null, [RosterMember, RosterMember with { Id = "duplicate" }]);

        Assert.Equal("Guest", label.DisplayName);
        Assert.True(label.Guest);
    }
}
