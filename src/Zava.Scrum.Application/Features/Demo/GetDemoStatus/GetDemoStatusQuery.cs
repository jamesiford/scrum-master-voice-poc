using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Demo.GetDemoStatus;

/// <summary>How much demo state exists.</summary>
/// <param name="TeamId">Team id.</param>
public sealed record GetDemoStatusQuery(string TeamId) : IQuery<DemoStatus>;

/// <summary>Counts of demo records.</summary>
/// <param name="Sessions">Standup sessions.</param>
/// <param name="FollowUps">Follow-ups.</param>
/// <param name="Cards">Cards sent.</param>
public sealed record DemoStatus(int Sessions, int FollowUps, int Cards);
