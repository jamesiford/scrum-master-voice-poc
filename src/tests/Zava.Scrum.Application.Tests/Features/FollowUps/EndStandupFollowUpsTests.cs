using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Standups.EndStandup;
using Zava.Scrum.Application.Features.Standups.MarkAbsent;
using Zava.Scrum.Application.Features.Standups.RecordStandupEvent;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Standups;
using Microsoft.Extensions.DependencyInjection;

namespace Zava.Scrum.Application.Tests.Features.FollowUps;

public sealed class EndStandupFollowUpsTests
{
    [Fact]
    public async Task End_WithCommitmentAndAbsence_CreatesFollowUpsAndCardsOnce()
    {
        await using var h = await StandupHarness.CreateAsync();
        await h.Sender.Send(new RecordStandupEventCommand(h.SessionId, StandupEventType.Commitment, "m-priya", "Finishing retries today", [1]));
        await h.Sender.Send(new MarkAbsentCommand(h.SessionId, "m-marcus"));

        await h.Sender.Send(new EndStandupCommand(h.SessionId));
        await h.Sender.Send(new EndStandupCommand(h.SessionId));

        var followUps = await h.Get<IFollowUpRepository>().ListBySessionAsync(h.SessionId, default);
        Assert.Contains(followUps, f => f.Kind == FollowUpKind.Followup && f.MemberId == "m-priya" && f.Changes.Count == 1 && f.Status == FollowUpStatus.Sent);
        Assert.Contains(followUps, f => f.Kind == FollowUpKind.Absent && f.MemberId == "m-marcus");
        var cards = ((RecordingTeamsMessenger)h.Get<ITeamsMessenger>()).Sent;
        Assert.Single(cards, c => c.Kind == CardKind.Recap);
        Assert.Equal(followUps.Count, cards.Count(c => c.Kind is CardKind.FollowUp or CardKind.Absent or CardKind.Escalation));
    }
}
