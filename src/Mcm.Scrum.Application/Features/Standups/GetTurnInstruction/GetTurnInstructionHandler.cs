using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Application.Features.Teams;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Standups;
using Mcm.Scrum.Domain.Voice;

namespace Mcm.Scrum.Application.Features.Standups.GetTurnInstruction;

/// <summary>Fills a turn template with facts from the session. The model only phrases it; every fact comes from code (rule 3).</summary>
internal sealed class GetTurnInstructionHandler(ISessionRepository sessions, ITeamRepository teams, IPromptStore prompts, IClock clock)
    : IRequestHandler<GetTurnInstructionQuery, Result<string>>
{
    public async Task<Result<string>> Handle(GetTurnInstructionQuery request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result<string>.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        var template = await prompts.GetAsync(TemplateName(request.Reason), ct);
        if (template is null)
        {
            return Result<string>.Failure(Error.NotFound("prompt.not_found", $"Prompt '{TemplateName(request.Reason)}' is missing from agents/instructions."));
        }

        var team = await teams.GetAsync(session.TeamId, ct);
        return Result<string>.Success(PromptTemplate.Render(template, Values(session, team?.Name ?? "the team", request)));
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
        };
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
