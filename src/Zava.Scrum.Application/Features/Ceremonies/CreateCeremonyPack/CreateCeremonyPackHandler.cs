using System.Text.Json;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.FollowUps;
using Zava.Scrum.Application.Features.Teams;
using Zava.Scrum.Domain.Ceremonies;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Application.Features.Ceremonies.CreateCeremonyPack;

/// <summary>
/// Computes the pack in Domain code, lets the model rephrase only the summary (rule 3), and reuses the follow-up, card and
/// approval path of the standup so the approval guard is the same one (F7).
/// </summary>
internal sealed class CreateCeremonyPackHandler(
    ITeamRepository teams,
    IMemberRepository members,
    IWorkItemReader reader,
    IIterationReader iterations,
    ISessionRepository sessions,
    IFollowUpRepository followUps,
    ITeamsMessenger messenger,
    IPublisher publisher,
    IDraftingAgent agent,
    IClock clock) : IRequestHandler<CreateCeremonyPackCommand, Result<Guid>>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<Result<Guid>> Handle(CreateCeremonyPackCommand request, CancellationToken ct)
    {
        var team = await teams.GetAsync(request.TeamId, ct);
        if (team is null)
        {
            return Result<Guid>.Failure(TeamErrors.NotFound(request.TeamId));
        }

        var sprint = await reader.GetCurrentSprintAsync(team, ct);
        if (!sprint.IsSuccess)
        {
            return Result<Guid>.Failure(sprint.Error!);
        }

        var roster = await members.ListAsync(team.Id, ct);
        var next = await iterations.GetNextIterationPathAsync(team, ct);
        var teamSessions = (await sessions.ListAsync(ct)).Where(s => s.TeamId == team.Id).ToList();
        var pack = request.Kind switch
        {
            CeremonyKind.SprintReview => ReviewPackBuilder.Build(sprint.Value),
            _ => RetroPackBuilder.Build(sprint.Value, teamSessions, roster, next),
        };

        var now = clock.UtcNow;
        var summary = await PhraseAsync(pack, sprint.Value, ct);
        var session = StandupSession.Prepare(Guid.NewGuid(), team.Id, now, null, request.Kind);
        await sessions.SaveAsync(session, ct);
        await session.PublishEventsAsync(publisher, ct);

        var followUp = FollowUp.Create(
            Guid.NewGuid(), team.Id, session.Id, null, pack.FollowUpKind, summary, pack.Changes, DateOnly.FromDateTime(now.UtcDateTime), now, request.Kind, forScrumMaster: true);
        followUp.MarkSent();
        await followUps.SaveAsync(followUp, ct);

        var card = FollowUpCards.Initial(followUp, "the team");
        await messenger.SendAsync(card with { Facts = [.. pack.Facts.Select(f => new CardFact(f.Label, f.Value)), .. pack.Items.Select(i => new CardFact("Proposed", i.Text))] }, ct);
        return Result<Guid>.Success(followUp.Id);
    }

    // The model may rephrase the summary, but it is dropped if it names an item outside the sprint or a number the pack did not compute.
    private async Task<string> PhraseAsync(CeremonyPack pack, SprintSnapshot sprint, CancellationToken ct)
    {
        var reply = await agent.DraftJsonAsync("ceremony-pack", JsonSerializer.Serialize(new { ceremony = pack.Kind.ToString(), pack.IterationPath, pack.Summary, pack.Facts, items = pack.Items.Select(i => i.Text) }, Json), ct);
        var phrased = Summary(reply);
        var known = sprint.Items.Select(i => i.Id).ToHashSet();
        return phrased is not null && WorkItemIdGuard.IsGrounded(phrased, known) && pack.IsGrounded(phrased) ? phrased : pack.Summary;
    }

    private static string? Summary(string? reply)
    {
        if (reply is null)
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(reply);
            return doc.RootElement.TryGetProperty("summary", out var s) && s.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(s.GetString())
                ? s.GetString()!.Trim()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
