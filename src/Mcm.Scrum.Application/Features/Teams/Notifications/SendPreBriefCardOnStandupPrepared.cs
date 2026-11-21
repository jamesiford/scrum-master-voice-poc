using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Features.Teams.Notifications;

/// <summary>Sends the pre-brief card to the Scrum Master when a standup is prepared.</summary>
internal sealed class SendPreBriefCardOnStandupPrepared(ISessionRepository sessions, ITeamsMessenger messenger) : INotificationHandler<StandupPrepared>
{
    public async Task Handle(StandupPrepared domainEvent, CancellationToken ct)
    {
        var session = await sessions.GetAsync(domainEvent.SessionId, ct);
        if (session?.PreBrief is not { } brief)
        {
            return;
        }

        var facts = brief.Members
            .Select(m => new CardFact(m.DisplayName, $"{m.Items.Count} items, {m.Items.Count(i => i.Flags.Count > 0)} flagged"))
            .ToList();
        var text = string.IsNullOrWhiteSpace(brief.Summary) ? $"Sprint {brief.IterationPath} pre-brief." : brief.Summary;
        await messenger.SendAsync(new TeamsMessage(CardKind.PreBrief, session.TeamId, session.Id, $"prebrief:{session.Id}", $"Pre-brief: {brief.IterationPath}", text, facts), ct);
    }
}
