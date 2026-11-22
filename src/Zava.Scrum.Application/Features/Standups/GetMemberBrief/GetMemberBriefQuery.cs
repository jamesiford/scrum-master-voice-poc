using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Features.Standups.GetMemberBrief;

/// <summary>Reads one member's brief from the session's pre-brief.</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="MemberId">Member id.</param>
public sealed record GetMemberBriefQuery(Guid SessionId, string MemberId) : IQuery<MemberBrief>;
