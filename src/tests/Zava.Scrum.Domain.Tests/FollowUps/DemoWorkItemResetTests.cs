using Zava.Scrum.Domain.FollowUps;
using System.Security.Cryptography;
using System.Text;

namespace Zava.Scrum.Domain.Tests.FollowUps;

/// <summary>Approval hashes bind every reset field, including clears, without depending on dictionary insertion order.</summary>
public sealed class DemoWorkItemResetTests
{
    [Fact]
    public void ProposalHash_OrdinaryProposal_PreservesPreviousCanonicalBytes()
    {
        var ordinary = new ProposedChange(7, ChangeOp.State, "Active", "unchanged");
        var historical = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("7|State|Active|\n")));

        Assert.Equal(historical, ProposalHash.Of([ordinary]));
        Assert.Equal(historical, ProposalHash.Of([ordinary with { DemoReset = null }]));
    }

    [Fact]
    public void ProposalHash_ChangedBaselineOrClear_DiffersButReorderedFieldsMatch()
    {
        var reset = new DemoWorkItemReset(Guid.NewGuid(), "Demo", "Demo\\Area", "demo-key-one",
            new Dictionary<string, string?> { ["title"] = "original", ["remaining"] = null });
        var proposal = new ProposedChange(7, ChangeOp.ResetDemoItem, "baseline", "seed", DemoReset: reset);
        var hash = ProposalHash.Of([proposal]);

        Assert.Equal(hash, ProposalHash.Of([proposal with { DemoReset = reset with
        {
            Fields = new Dictionary<string, string?> { ["remaining"] = null, ["title"] = "original" },
        } }]));
        Assert.NotEqual(hash, ProposalHash.Of([proposal with { DemoReset = reset with
        {
            Fields = new Dictionary<string, string?> { ["title"] = "changed", ["remaining"] = null },
        } }]));
        Assert.NotEqual(hash, ProposalHash.Of([proposal with { DemoReset = reset with
        {
            Fields = new Dictionary<string, string?> { ["title"] = "original", ["remaining"] = string.Empty },
        } }]));
    }
}
