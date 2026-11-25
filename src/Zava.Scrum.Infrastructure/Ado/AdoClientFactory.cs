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

    /// <summary>Gets the work item writer for an organisation.</summary>
    /// <param name="organisationUrl">The organisation URL.</param>
    /// <returns>The writer, cached with its client.</returns>
    AdoWorkItemWriter WriterFor(string organisationUrl);
}

/// <summary>
/// One <see cref="HttpClient"/> per organisation, created lazily and cached. Reads and the connection test use the API host's managed
/// identity (ADR 0015 S4). The organisation URL is validated by <see cref="AdoOrganisation"/> before any client exists, so a team cannot point
/// the API, and its token, at an arbitrary host. Nothing here is keyed to a team, so a team resolves its own organisation through its settings.
/// </summary>
/// <remarks>PROD (prompt 5): writes use the approver's delegated token against the team's organisation.</remarks>
public sealed class AdoClientFactory(TokenCredential credential, IOptions<AdoOptions> options, IOptions<DemoOptions> demo, ILoggerFactory loggers) : IAdoClientFactory, IDisposable
{
    private const string AdoScope = "499b84ac-1321-427f-aa17-267ca6975798/.default";
    private readonly ConcurrentDictionary<string, Lazy<HttpClient>> _clients = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Lazy<AdoRestClient>> _readers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Lazy<AdoWorkItemWriter>> _writers = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public HttpClient For(string organisationUrl) => _clients.GetOrAdd(Normalise(organisationUrl), Create).Value;

    /// <inheritdoc />
    public AdoRestClient ReaderFor(string organisationUrl)
    {
        var org = Normalise(organisationUrl);
        return _readers.GetOrAdd(org, o => new Lazy<AdoRestClient>(() => new AdoRestClient(For(o), demo, loggers.CreateLogger<AdoRestClient>()))).Value;
    }

    /// <inheritdoc />
    public AdoWorkItemWriter WriterFor(string organisationUrl)
    {
        var org = Normalise(organisationUrl);
        return _writers.GetOrAdd(org, o => new Lazy<AdoWorkItemWriter>(() => new AdoWorkItemWriter(For(o), demo, loggers.CreateLogger<AdoWorkItemWriter>()))).Value;
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
    }

    private static string Normalise(string url) =>
        AdoOrganisation.Normalise(url) ?? throw new ArgumentException($"'{url}' is not an Azure DevOps organisation URL.", nameof(url));

    private Lazy<HttpClient> Create(string org) => new(() =>
    {
        var ado = options.Value;
        DelegatingHandler auth = ado.AuthMode == "Pat"
            ? new PatAuthHandler(ado.Pat ?? throw new InvalidOperationException("Ado:Pat is required when Ado:AuthMode is Pat."))
            : new BearerTokenHandler(credential, AdoScope);
        auth.InnerHandler = new HttpClientHandler();
        return new HttpClient(auth, disposeHandler: true) { BaseAddress = new Uri(org + "/"), Timeout = TimeSpan.FromSeconds(100) };
    });
}
