using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Standups.StartStandup;

/// <summary>Marks a prepared session live and fixes the timebox for this run.</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="Mode">Bridge mode: <c>simulated</c> or <c>acs</c>.</param>
/// <param name="JoinUrl">Teams join URL (acs mode).</param>
/// <param name="StandupMinutes">Timebox override; the team's setting when <see langword="null"/>.</param>
public sealed record StartStandupCommand(Guid SessionId, string Mode, string? JoinUrl = null, int? StandupMinutes = null) : ICommand;
