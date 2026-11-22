using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.FollowUps.SendFollowUps;
using Zava.Scrum.Application.Features.Standups.EndStandup;
using Zava.Scrum.Application.Features.Standups.RecordStandupEvent;
using Zava.Scrum.Application.Features.Standups.ResolveUnblocker;
using Zava.Scrum.Application.Features.Teams.SetRecapChannel;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Standups;
using Microsoft.Extensions.DependencyInjection;

namespace Zava.Scrum.Application.Tests.Features;

public sealed class UnblockAndRecapTests
{
    private static async Task<(StandupHarness H, FakeEmailSender Email)> CreateAsync()
    {
        var email = new FakeEmailSender(enabled: true);
        var members = new InMemoryMemberRepository();
        await members.ReplaceAsync("t1", [
            new Member("m-priya", "t1", "priya", "Priya S.", "priya@x.com", "Priya", 1, true),
            new Member("m-marcus", "t1", "marcus", "Marcus O.", "marcus@x.com", "Marcus", 2, true),
        ], default);
        var h = await StandupHarness.CreateAsync(configure: s =>
        {
            s.AddSingleton<IEmailSender>(email);
            s.AddSingleton<IMemberRepository>(members);
        });
        return (h, email);
    }

    [Fact]
    public async Task ResolveUnblocker_AnswerNamesARosterMember_StoresItOnTheOpenBlocker()
    {
        var (h, _) = await CreateAsync();
        await using var _ = h;
        await h.Sender.Send(new RecordStandupEventCommand(h.SessionId, StandupEventType.Blocker, "m-priya", "waiting on the vendor", [1]));

        var named = await h.Sender.Send(new ResolveUnblockerCommand(h.SessionId, "m-priya", "Marcus could help"));

        Assert.Equal("m-marcus", named.Value);
        Assert.Equal("m-marcus", (await h.LoadAsync()).Events.Single(e => e.Type == StandupEventType.Blocker).UnblockerMemberId);
    }

    [Fact]
    public async Task ResolveUnblocker_NobodyNamed_LeavesTheBlockerUntouched()
    {
        var (h, _) = await CreateAsync();
        await using var _ = h;
        await h.Sender.Send(new RecordStandupEventCommand(h.SessionId, StandupEventType.Blocker, "m-priya", "waiting on the vendor", [1]));

        var named = await h.Sender.Send(new ResolveUnblockerCommand(h.SessionId, "m-priya", "nobody, it is the vendor"));

        Assert.Null(named.Value);
        Assert.Null((await h.LoadAsync()).Events.Single(e => e.Type == StandupEventType.Blocker).UnblockerMemberId);
    }

    [Fact]
    public async Task End_BlockerWithUnblocker_SendsTheUnblockerACardAndAnEmail()
    {
        var (h, email) = await CreateAsync();
        await using var _ = h;
        await h.Sender.Send(new RecordStandupEventCommand(h.SessionId, StandupEventType.Blocker, "m-priya", "waiting on the vendor", [1]));
        await h.Sender.Send(new ResolveUnblockerCommand(h.SessionId, "m-priya", "Marcus"));

        await h.Sender.Send(new EndStandupCommand(h.SessionId));

        var unblock = Assert.Single(await h.Get<IFollowUpRepository>().ListBySessionAsync(h.SessionId, default), f => f.Kind == FollowUpKind.Unblock);
        Assert.Equal("m-marcus", unblock.MemberId);
        var card = Assert.Single(((RecordingTeamsMessenger)h.Get<ITeamsMessenger>()).Sent, c => c.Kind == CardKind.Unblock);
        Assert.Equal("m-marcus", card.MemberId);
        var mail = Assert.Single(email.Sent, m => m.FollowUpId == unblock.Id);
        Assert.Equal("marcus@x.com", mail.To);
        Assert.Contains("help unblock", mail.Subject, StringComparison.Ordinal);
    }

    [Fact]
    public async Task End_AbsentMember_GetsAnEmailAsWellAsACard()
    {
        var (h, email) = await CreateAsync();
        await using var _ = h;
        await h.Sender.Send(new Zava.Scrum.Application.Features.Standups.MarkAbsent.MarkAbsentCommand(h.SessionId, "m-marcus"));

        await h.Sender.Send(new EndStandupCommand(h.SessionId));

        Assert.Contains(email.Sent, m => m.To == "marcus@x.com");
        Assert.Contains(((RecordingTeamsMessenger)h.Get<ITeamsMessenger>()).Sent, c => c.Kind == CardKind.Absent && c.MemberId == "m-marcus");
    }

    [Fact]
    public async Task End_Recap_IsRoutedToTheRecapChannelOnce()
    {
        var (h, _) = await CreateAsync();
        await using var _ = h;
        await h.Sender.Send(new SetRecapChannelCommand("t1", "19:channel@thread.tacv2"));

        await h.Sender.Send(new EndStandupCommand(h.SessionId));

        var recap = Assert.Single(((RecordingTeamsMessenger)h.Get<ITeamsMessenger>()).Sent, c => c.Kind == CardKind.Recap);
        Assert.Equal(CardRoute.Recap, recap.Route);
        Assert.Equal("19:channel@thread.tacv2", recap.RecapChannelKey);
    }

    [Fact]
    public async Task SetRecapChannel_UnknownTeam_IsNotFound()
    {
        var (h, _) = await CreateAsync();
        await using var _ = h;

        var result = await h.Sender.Send(new SetRecapChannelCommand("nope", "19:x"));

        Assert.Equal(Zava.Scrum.Domain.Common.ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task SendFollowUps_CalledTwice_StillCreatesTheUnblockFollowUpOnce()
    {
        var (h, _) = await CreateAsync();
        await using var _ = h;
        await h.Sender.Send(new RecordStandupEventCommand(h.SessionId, StandupEventType.Blocker, "m-priya", "waiting", [1]));
        await h.Sender.Send(new ResolveUnblockerCommand(h.SessionId, "m-priya", "Marcus"));

        await h.Sender.Send(new SendFollowUpsCommand(h.SessionId));
        await h.Sender.Send(new SendFollowUpsCommand(h.SessionId));

        Assert.Single(await h.Get<IFollowUpRepository>().ListBySessionAsync(h.SessionId, default), f => f.Kind == FollowUpKind.Unblock);
    }
}
