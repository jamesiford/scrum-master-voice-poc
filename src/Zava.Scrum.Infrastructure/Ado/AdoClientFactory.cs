using System.Collections.Concurrent;
using Azure.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Infrastructure.Http;

namespace Zava.Scrum.Infrastructure.Ado;

/// <summary>Gives each Azure DevOps organisation its own <see cref="HttpClient"/> and credential (ADR 0018).</summary>
public interface IAdoClientFactory
{
    /// <summary>Gets the client for an organisation, created on first use and cached.</summary>
    /// <param name="organisationUrl">The organisation URL; normalised, and refused unless it is a Microsoft-hosted Azure DevOps address.</param>
    /// <returns>A client whose base address is the organisation.</returns>
    HttpClient For(string organisationUrl);

    /// <summary>Gets the work item reader for an organisation.</summary>
    /// <param name="organisationUrl">The organisation URL.</param>
    /// <returns>The reader, cached with its client.</returns>
    AdoRestClient ReaderFor(string organisationUrl);

    /// <summary>Gets a work item writer that calls the organisation with one person's own access token.</summary>
    /// <param name="organisationUrl">The organisation URL.</param>
    /// <param name="accessToken">The approver's delegated Azure DevOps token. It is used for this writer only and is never cached.</param>
    /// <returns>A writer for one write; dispose nothing, it shares the factory's connection pool.</returns>
    AdoWorkItemWriter DelegatedWriterFor(string organisationUrl, string accessToken);
}

/// <summary>
/// One <see cref="HttpClient"/> per organisation, created lazily and cached. Reads and the connection test use the API host's managed
/// identity (ADR 0015 S4). The organisation URL is validated by <see cref="AdoOrganisation"/> before any client exists, so a team cannot point
/// the API, and its token, at an arbitrary host. Nothing here is keyed to a team, so a team resolves its own organisation through its settings.
/// </summary>
/// <param name="credential">The managed identity credential.</param>
/// <param name="options">Azure DevOps options.</param>
/// <param name="demo">Demo options.</param>
/// <param name="loggers">Logger factory.</param>
/// <param name="transport">Creates the innermost handler; <see langword="null"/> uses the network. Tests pass a fake here.</param>
/// <remarks>Writes never use this credential: <see cref="DelegatedWriterFor"/> carries the approver's own token (ADR 0020).</remarks>
public sealed class AdoClientFactory(TokenCredential credential, IOptions<AdoOptions> options, IOptions<DemoOptions> demo, ILoggerFactory loggers, Func<HttpMessageHandler>? transport = null) : IAdoClientFactory, IDisposable
{
    private const string AdoScope = "499b84ac-1321-427f-aa17-267ca6975798/.default";
    private readonly ConcurrentDictionary<string, Lazy<HttpClient>> _clients = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Lazy<AdoRestClient>> _readers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lazy<HttpMessageHandler> _delegatedTransport = new(() => transport?.Invoke() ?? new SocketsHttpHandler());

    /// <inheritdoc />
    public HttpClient For(string organisationUrl) => _clients.GetOrAdd(Normalise(organisationUrl), Create).Value;

    /// <inheritdoc />
    public AdoRestClient ReaderFor(string organisationUrl)
    {
        var org = Normalise(organisationUrl);
        return _readers.GetOrAdd(org, o => new Lazy<AdoRestClient>(() => new AdoRestClient(For(o), demo, loggers.CreateLogger<AdoRestClient>()))).Value;
    }

    /// <inheritdoc />
    public AdoWorkItemWriter DelegatedWriterFor(string organisationUrl, string accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        var org = Normalise(organisationUrl);
        var client = new HttpClient(_delegatedTransport.Value, disposeHandler: false) { BaseAddress = new Uri(org + "/"), Timeout = TimeSpan.FromSeconds(100) };
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return new AdoWorkItemWriter(client, demo, loggers.CreateLogger<AdoWorkItemWriter>());
    }

    /// <summary>Gets how many organisations have a client (for tests and diagnostics).</summary>
    public int OrganisationCount => _clients.Count(c => c.Value.IsValueCreated);

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var client in _clients.Values.Where(c => c.IsValueCreated))
        {
            client.Value.Dispose();
        }

        if (_delegatedTransport.IsValueCreated)
        {
            _delegatedTransport.Value.Dispose();
        }
    }

    private static string Normalise(string url) =>
        AdoOrganisation.Normalise(url) ?? throw new ArgumentException($"'{url}' is not an Azure DevOps organisation URL.", nameof(url));

    private Lazy<HttpClient> Create(string org) => new(() =>
    {
        var ado = options.Value;
        DelegatingHandler auth = ado.AuthMode == "Pat"
            ? new PatAuthHandler(ado.Pat ?? throw new InvalidOperationException("Ado:Pat is required when Ado:AuthMode is Pat."))
            : new BearerTokenHandler(credential, AdoScope);
        auth.InnerHandler = transport?.Invoke() ?? new HttpClientHandler();
        return new HttpClient(auth, disposeHandler: true) { BaseAddress = new Uri(org + "/"), Timeout = TimeSpan.FromSeconds(100) };
    });
}
