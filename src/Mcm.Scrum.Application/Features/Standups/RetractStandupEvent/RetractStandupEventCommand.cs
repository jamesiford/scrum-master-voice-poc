using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Standups.RetractStandupEvent;

/// <summary>Marks an event as corrected by its speaker. It stays in the audit trail but no rule, card or follow-up uses it.</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="EventId">The event to retract.</param>
public sealed record RetractStandupEventCommand(Guid SessionId, Guid EventId) : ICommand;
