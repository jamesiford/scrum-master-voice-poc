using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Domain.Tests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_HasNoError()
    {
        var result = Result<int>.Success(3);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value);
    }

    [Fact]
    public void Value_OnFailure_Throws()
    {
        var result = Result<int>.Failure(Error.NotFound("x.missing", "missing"));

        Assert.False(result.IsSuccess);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
