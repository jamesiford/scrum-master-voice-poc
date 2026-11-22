using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace Zava.Scrum.Infrastructure.Teams;

/// <summary>Records emails in the collection store instead of sending them.</summary>
/// <remarks>DEMO: the outbox is the "sent" evidence. PROD: Azure Communication Services Email (needs a verified sender domain) or Graph <c>sendMail</c>.</remarks>
public sealed class OutboxEmailSender(ICollectionStore store, IConfiguration configuration) : IEmailSender, IDisposable
{
    private const string Name = "emails.json";
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    /// <inheritdoc />
    public bool Enabled => configuration.GetValue<bool>("Features:EmailFollowUps");

    /// <inheritdoc />
    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        await _gate.WaitAsync(ct);
        try
        {
            var all = await store.ReadAsync(Name, () => new List<EmailMessage>(), ct);
            all.Insert(0, message);
            await store.WriteAsync(Name, all, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmailMessage>> ListAsync(CancellationToken ct) =>
        await store.ReadAsync(Name, () => new List<EmailMessage>(), ct);
}
