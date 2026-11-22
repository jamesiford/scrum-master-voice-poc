using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Teams.UpdateMemberAvailability;

/// <summary>Sets how much of a normal sprint a member is available for; sprint planning scales their capacity by it.</summary>
/// <param name="TeamId">Team id.</param>
/// <param name="MemberId">Member id.</param>
/// <param name="Percent">Availability, 0 to 100.</param>
public sealed record UpdateMemberAvailabilityCommand(string TeamId, string MemberId, int Percent) : ICommand;
