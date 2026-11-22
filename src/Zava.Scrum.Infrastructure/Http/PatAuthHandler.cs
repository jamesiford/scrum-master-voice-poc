using System.Net.Http.Headers;
using System.Text;

namespace Mcm.Scrum.Infrastructure.Http;

/// <summary>Adds Basic auth for an Azure DevOps personal access token (fallback when Entra is not possible, SPEC 9.1).</summary>
/// <param name="pat">The token, from user secrets or a Container Apps secret.</param>
public sealed class PatAuthHandler(string pat) : DelegatingHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + pat)));
        return base.SendAsync(request, cancellationToken);
    }
}
