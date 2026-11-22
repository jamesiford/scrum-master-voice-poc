using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Features.Ceremonies.CreateCeremonyPack;

/// <summary>
/// Builds the pack for a ceremony (planning, review or retrospective) and sends it to the Scrum Master as a follow-up with
/// Approve all and Reject. Nothing is written to Azure DevOps until it is approved (F7). Returns the follow-up id.
/// </summary>
/// <param name="TeamId">Team id.</param>
/// <param name="Kind">The ceremony; the daily scrum has no pack.</param>
public sealed record CreateCeremonyPackCommand(string TeamId, CeremonyKind Kind) : ICommand<Guid>;
