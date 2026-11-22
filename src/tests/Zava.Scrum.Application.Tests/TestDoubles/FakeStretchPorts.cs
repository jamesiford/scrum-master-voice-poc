using Zava.Scrum.Application.Abstractions.Ports;

namespace Zava.Scrum.Application.Tests.TestDoubles;

internal sealed class FakeEmailSender(bool enabled = false) : IEmailSender
{
    public List<EmailMessage> Sent { get; } = [];

    public bool Enabled => enabled;

    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EmailMessage>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<EmailMessage>>(Sent);
}

internal sealed class FakeDraftingAgent(string? reply) : IDraftingAgent
{
    public Task<string?> DraftJsonAsync(string promptName, string inputJson, CancellationToken ct) => Task.FromResult(reply);
}
