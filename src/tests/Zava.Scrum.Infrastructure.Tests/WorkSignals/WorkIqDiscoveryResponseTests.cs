using Zava.Scrum.Infrastructure.WorkSignals;

namespace Zava.Scrum.Infrastructure.Tests.WorkSignals;

public sealed class WorkIqDiscoveryResponseTests
{
    [Theory]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"tools\":[]}}")]
    [InlineData("event: message\ndata: {\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"tools\":[]}}\n\n")]
    [InlineData("data: {\"jsonrpc\":\"2.0\",\"method\":\"notification\"}\n\ndata: {\"jsonrpc\":\"2.0\",\"id\":2,\ndata: \"result\":{\"tools\":[]}}\n\n")]
    public void Parse_JsonOrSse_ReturnsDetachedMatchingResult(string body)
    {
        var result = WorkIqDiscoveryResponse.Parse(body, 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.GetProperty("tools").GetArrayLength());
    }

    [Theory]
    [InlineData("")]
    [InlineData("data: not-json\n\n")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"tools\":[]}}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":2,\"error\":{\"code\":-1}}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":2,\"result\":null}")]
    [InlineData("{\"jsonrpc\":\"1.0\",\"id\":2,\"result\":{\"tools\":[]}}")]
    [InlineData("{\"jsonrpc\":null,\"id\":2,\"result\":{\"tools\":[]}}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":\"2\",\"result\":{\"tools\":[]}}")]
    public void Parse_InvalidOrError_ReturnsVisibleFailure(string body)
    {
        var result = WorkIqDiscoveryResponse.Parse(body, 2);

        Assert.False(result.IsSuccess);
        Assert.Equal("signals.discovery_invalid", result.Error!.Code);
    }
}
