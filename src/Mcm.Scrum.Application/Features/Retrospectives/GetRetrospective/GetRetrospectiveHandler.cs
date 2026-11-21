using System.Text;
using System.Text.Json;
using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Application.Features.Reports;
using Mcm.Scrum.Application.Features.Teams;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Planning;

namespace Mcm.Scrum.Application.Features.Retrospectives.GetRetrospective;

/// <summary>Builds the retrospective skeleton in code and lets the model phrase a short summary above it.</summary>
internal sealed class GetRetrospectiveHandler(
    ITeamRepository teams,
    IMemberRepository members,
    IWorkItemReader reader,
    ISessionRepository sessions,
    IDraftingAgent agent) : IRequestHandler<GetRetrospectiveQuery, Result<ReportDocument>>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<Result<ReportDocument>> Handle(GetRetrospectiveQuery request, CancellationToken ct)
    {
        var team = await teams.GetAsync(request.TeamId, ct);
        if (team is null)
        {
            return Result<ReportDocument>.Failure(TeamErrors.NotFound(request.TeamId));
        }

        var sprint = await reader.GetCurrentSprintAsync(team, ct);
        if (!sprint.IsSuccess)
        {
            return Result<ReportDocument>.Failure(sprint.Error!);
        }

        var prep = PlanningPlanner.Plan(sprint.Value, await members.ListAsync(team.Id, ct));
        var teamSessions = (await sessions.ListAsync(ct)).Where(s => s.TeamId == team.Id).ToList();
        var draft = RetrospectiveBuilder.Build(sprint.Value, teamSessions, prep);

        var reply = await agent.DraftJsonAsync("retrospective", JsonSerializer.Serialize(new { sprint = sprint.Value.IterationPath, draft }, Json), ct);
        var narrative = Narrative(reply) ?? "Here is what the sprint and the standups recorded. Discuss each column and pick one change to try next sprint.";
        var markdown = new StringBuilder($"# Retrospective: {sprint.Value.IterationPath}\n\n{narrative}\n");
        Section(markdown, "Start", draft.Start);
        Section(markdown, "Stop", draft.Stop);
        Section(markdown, "Continue", draft.Continue);
        return Result<ReportDocument>.Success(new ReportDocument("Retrospective", markdown.ToString(), narrative, reply is not null, false));
    }

    private static string? Narrative(string? reply)
    {
        if (reply is null)
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(reply);
            return doc.RootElement.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString()?.Trim() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void Section(StringBuilder sb, string title, IReadOnlyList<string> lines)
    {
        sb.Append("\n## ").Append(title).Append("\n\n");
        sb.Append(lines.Count == 0 ? "- Nothing recorded.\n" : string.Concat(lines.Select(l => $"- {l}\n")));
    }
}
