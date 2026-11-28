using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Teams;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Voice;

namespace Zava.Scrum.Application.Features.Standups.GetTurnInstruction;

/// <summary>Fills a turn template with facts from the session. The model only phrases it; every fact comes from code (rule 3).</summary>
internal sealed class GetTurnInstructionHandler(ISessionRepository sessions, ITeamRepository teams, IPromptStore prompts, IRetroSettings retro, IClock clock)
    : IRequestHandler<GetTurnInstructionQuery, Result<string>>
{
    public async Task<Result<string>> Handle(GetTurnInstructionQuery request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result<string>.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        var name = session.Kind == CeremonyKind.Retrospective ? RetroTemplateName(session, request.Reason) : TemplateName(request.Reason);
        var templateTask = prompts.ResolveAsync(session.TeamId, name, ct);
        var teamTask = request.ParallelContextReads ? teams.GetAsync(session.TeamId, ct) : null;
        if (teamTask is not null)
        {
            await Task.WhenAll(templateTask, teamTask);
        }
        var template = (await templateTask)?.Text;
        if (template is null)
        {
            return Result<string>.Failure(Error.NotFound("prompt.not_found", $"Prompt '{name}' is missing from agents/instructions."));
        }

        if (session.Kind == CeremonyKind.Retrospective && request.Reason == SpeakReason.CircleBack)
        {
            const string requiredName = "turn-retro-circle-back-required";
            var required = await prompts.GetAsync(requiredName, ct);
            if (required is null)
            {
                return Result<string>.Failure(Error.NotFound("prompt.not_found", $"Prompt '{requiredName}' is missing from agents/instructions."));
            }
            template = $"{template}\n\n{required}";
        }

        var team = teamTask is null ? await teams.GetAsync(session.TeamId, ct) : await teamTask;
        return Result<string>.Success(PromptTemplate.Render(template, Values(session, team?.Name ?? "the team", request)));
    }

    // A retrospective reuses the standup's turns for step-away, silence, parking and the time warning, and has its own for the rest.
    private static string RetroTemplateName(StandupSession session, SpeakReason reason)
    {
        var actions = RetroAgenda.InActions(session);
        return reason switch
        {
            SpeakReason.Open => "turn-retro-open",
            SpeakReason.CallOn => actions ? "turn-retro-ask-actions" : "turn-retro-ask-member",
            SpeakReason.CircleBack => actions ? "turn-retro-ask-actions" : "turn-retro-circle-back",
            SpeakReason.Clarify => actions ? "turn-retro-confirm-owner" : "turn-retro-clarify-category",
            SpeakReason.RetroThemes => "turn-retro-themes",
            SpeakReason.Recap => "turn-retro-close",
            _ => TemplateName(reason),
        };
    }

    private static string TemplateName(SpeakReason reason) => reason switch
    {
        SpeakReason.Open => "turn-open",
        SpeakReason.CallOn => "turn-call-on",
        SpeakReason.Absent => "turn-absent",
        SpeakReason.Clarify => "turn-clarify",
        SpeakReason.Park => "turn-park",
        SpeakReason.TimeWarning => "turn-time-warning",
        SpeakReason.Addressed => "turn-addressed",
        SpeakReason.StepAway => "turn-step-away",
        SpeakReason.NoResponse => "turn-no-response",
        SpeakReason.CircleBack => "turn-circle-back",
        SpeakReason.Unblocker => "turn-unblocker",
        _ => "turn-recap",
    };

    private Dictionary<string, string> Values(StandupSession session, string teamName, GetTurnInstructionQuery request)
    {
        var member = session.PreBrief?.Members.FirstOrDefault(m => m.MemberId == request.MemberId);
        var elapsed = session.StartedAt is { } s ? (clock.UtcNow - s).TotalMinutes : 0;
        return new Dictionary<string, string>
        {
            ["teamName"] = teamName,
            ["sprintGoal"] = session.PreBrief?.SprintGoal ?? "not set",
            ["daysLeft"] = (session.PreBrief?.DaysLeft ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["firstName"] = member is null ? string.Empty : Names.First(member.DisplayName),
            ["question"] = request.Question ?? string.Empty,
            ["notes"] = request.Notes ?? string.Empty,
            ["minutesLeft"] = Math.Max(1, (int)Math.Round(session.StandupMinutes - elapsed)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["blockers"] = Lines(session, StandupEventType.Blocker, "none raised"),
            ["parked"] = Lines(session, StandupEventType.ParkingLot, "none"),
            ["absent"] = AbsentNames(session),
            ["noResponse"] = SilentNames(session),
            ["commitments"] = session.ActiveEvents.Count(e => e.Type == StandupEventType.Commitment).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["summary"] = session.PreBrief?.Summary ?? string.Empty,
            ["retroPrompt"] = RetroFormats.Prompt(retro.Format),
            ["themes"] = RetroLines(session),
            ["actions"] = ActionLines(session),
            ["noteCount"] = RetroSummary.Build(session).Notes.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
    }

    private static string RetroLines(StandupSession session)
    {
        var themes = RetroSummary.Build(session).Themes;
        return themes.Count == 0 ? "no strong themes, the notes were all different" : string.Join("; ", themes.Select(t => $"{t.Name} ({t.Count} notes)"));
    }

    private static string ActionLines(StandupSession session)
    {
        var actions = RetroSummary.Build(session).Actions;
        return actions.Count == 0 ? "none agreed" : string.Join("; ", actions.Select(a => $"{a.Text}, owned by {Names.First(a.OwnerName)}"));
    }

    private static string Lines(StandupSession session, StandupEventType type, string none)
    {
        var texts = session.ActiveEvents.Where(e => e.Type == type).Select(e => e.Text).ToList();
        return texts.Count == 0 ? none : string.Join("; ", texts);
    }

    private static string SilentNames(StandupSession session)
    {
        var names = session.PreBrief?.Members
            .Where(m => session.NoResponseCount(m.MemberId) > 0 && !session.HasSpoken(m.MemberId))
            .Select(m => Names.First(m.DisplayName))
            .ToList() ?? [];
        return names.Count == 0 ? "nobody" : string.Join(" and ", names);
    }

    private static string AbsentNames(StandupSession session)
    {
        var names = session.PreBrief?.Members
            .Where(m => session.Attendance.TryGetValue(m.MemberId, out var a) && a == Attendance.Absent)
            .Select(m => Names.First(m.DisplayName))
            .ToList() ?? [];
        return names.Count == 0 ? "nobody" : string.Join(" and ", names);
    }
}
