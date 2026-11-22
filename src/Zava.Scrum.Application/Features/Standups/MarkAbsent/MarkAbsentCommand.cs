using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Standups.MarkAbsent;

/// <summary>Marks a member absent today.</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="MemberId">Member id.</param>
public sealed record MarkAbsentCommand(Guid SessionId, string MemberId) : ICommand;
