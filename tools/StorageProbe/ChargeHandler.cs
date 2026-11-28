using Microsoft.Azure.Cosmos;

namespace StorageProbe;

/// <summary>Accumulates actual response charges so composed repository reads include publication metadata.</summary>
internal sealed class ChargeHandler : RequestHandler
{
    /// <summary>Gets accumulated request units for this sequential probe.</summary>
    internal double Total { get; private set; }

    /// <inheritdoc />
    public override async Task<ResponseMessage> SendAsync(RequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        Total += response.Headers.RequestCharge;
        return response;
    }
}
