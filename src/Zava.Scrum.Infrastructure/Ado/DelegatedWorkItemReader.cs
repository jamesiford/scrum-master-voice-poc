using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Behaviors;
using Zava.Scrum.Application.Features.Reports.AnswerCopilotQuestion;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Infrastructure.Ado;

/// <summary>Reuses the real REST sprint reader with the asker's uncached token and no service-identity fallback.</summary>
internal sealed class DelegatedWorkItemReader(IAdoClientFactory factory, IOptions<DemoOptions> demo, ILoggerFactory loggers) : IDelegatedWorkItemReader
{
    /// <inheritdoc />
    public async Task<Result<SprintSnapshot>> GetCurrentSprintAsync(Team team, DelegatedAdoAccess access, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(team);
        ArgumentNullException.ThrowIfNull(access);
        Activity.Current?.SetTag(ScrumTelemetry.TeamTag, team.Id);
        using var http = factory.DelegatedClientFor(team.Ado.Org, access.AccessToken);
        try
        {
            var result = await new AdoRestClient(http, demo, loggers.CreateLogger<AdoRestClient>()).GetCurrentSprintAsync(team, ct);
            return !result.IsSuccess && result.Error!.Code == "ado.unauthorized"
                ? Result<SprintSnapshot>.Failure(CopilotErrors.AccessDenied) : result;
        }
        catch (JsonException ex)
        {
            loggers.CreateLogger<DelegatedWorkItemReader>().LogWarning(ex, "Azure DevOps returned no readable sprint JSON for team {TeamId}", team.Id);
            return Result<SprintSnapshot>.Failure(Error.ExternalRejected("copilot.ado_response_invalid", "Azure DevOps did not return readable sprint data for your sign-in. Check your organisation access; no agent credential was used."));
        }
    }
}
