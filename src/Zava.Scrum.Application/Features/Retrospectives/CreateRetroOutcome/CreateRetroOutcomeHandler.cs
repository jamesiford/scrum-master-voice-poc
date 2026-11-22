using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.FollowUps;
using Zava.Scrum.Application.Features.Standups;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Features.Retrospectives.CreateRetroOutcome;

/// <summary>Builds the follow-up and the channel summary from the recorded events. Every figure is counted in code (rule 3).</summary>
internal sealed class CreateRetroOutcomeHandler(
    ISessionRepository sessions,
    ITeamRepository teams,
    IMemberRepository members,
    IIterationReader iterations,
    IFollowUpRepository followUps,
    ITeamsMessenger messenger,
    IRetroSettings settings,
    IClock clock) : IRequestHandler<CreateRetroOutcomeCommand, Result>
{
    public async Task<Result> Handle(CreateRetroOutcomeCommand request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        if ((await followUps.ListBySessionAsync(session.Id, ct)).Count > 0)
        {
            return Result.Success();
        }

        var team = await teams.GetAsync(session.TeamId, ct);
        var summary = RetroSummary.Build(session);
        var facts = Facts(summary);
        var headline = RetroOutcomePlanner.Headline(summary);
        var changes = summary.Actions.Count == 0
            ? []
            : RetroOutcomePlanner.Plan(summary, await members.ListAsync(session.TeamId, ct), await NextIterationAsync(session, team, ct));
        if (changes.Count > 0)
        {
            var now = clock.UtcNow;
            var followUp = FollowUp.Create(
                Guid.NewGuid(), session.TeamId, session.Id, null, FollowUpKind.RetroActions, headline, changes, DateOnly.FromDateTime(now.UtcDateTime), now, CeremonyKind.Retrospective, forScrumMaster: true);
            followUp.MarkSent();
            await followUps.SaveAsync(followUp, ct);
            var card = FollowUpCards.Initial(followUp, "the team");
            await messenger.SendAsync(card with { Facts = [.. facts, .. card.Facts] }, ct);
        }

        await messenger.SendAsync(new TeamsMessage(CardKind.Recap, session.TeamId, session.Id, $"retro:{session.Id}", "Retrospective summary", headline, facts, Route: CardRoute.Recap, RecapChannelKey: team?.RecapChannelKey), ct);
        return Result.Success();
    }

    private async Task<string> NextIterationAsync(StandupSession session, Domain.Teams.Team? team, CancellationToken ct) =>
        (team is null ? null : await iterations.GetNextIterationPathAsync(team, ct)) ?? session.PreBrief?.IterationPath ?? string.Empty;

    private List<CardFact> Facts(RetroSummary summary)
    {
        var facts = new List<CardFact>();
        foreach (var category in new[] { RetroCategory.Continue, RetroCategory.Stop, RetroCategory.Start })
        {
            var notes = summary.In(category);
            if (notes.Count > 0)
            {
                facts.Add(new CardFact(RetroFormats.Label(settings.Format, category), string.Join("; ", notes.Select(n => n.Text))));
            }
        }

        facts.AddRange(summary.Themes.Select(t => new CardFact("Theme", $"{t.Name} ({t.Count})")));
        facts.AddRange(summary.Actions.Select(a => new CardFact($"Action ({a.OwnerName})", a.Text)));
        return facts;
    }
}
