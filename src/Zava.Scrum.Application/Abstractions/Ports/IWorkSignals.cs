using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Delegated, read-only Work IQ evidence. No adapter is registered until the live spike passes (ADR 0027).</summary>
public interface IWorkSignals
{
    /// <summary>Reads only the context's channel for a code-verified board item, never private chats.</summary>
    /// <param name="context">The checked team, owner and bounded sources.</param>
    /// <param name="workItem">A work item present on this team's current board.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Source-labelled evidence, or an explicit access/provider failure.</returns>
    Task<Result<IReadOnlyList<WorkSignalEvidence>>> GetChannelActivityAsync(WorkSignalContext context, WorkItemId workItem, CancellationToken ct);

    /// <summary>Finds advisory helper evidence in the same channel; identities must match the verified roster.</summary>
    /// <param name="context">The checked team, owner and bounded sources.</param>
    /// <param name="topic">A topic associated with a code-verified board item.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Evidence only, never an assignment.</returns>
    Task<Result<IReadOnlyList<WorkSignalEvidence>>> SuggestHelpersAsync(WorkSignalContext context, string topic, CancellationToken ct);

    /// <summary>Reads existing notes for a stored meeting of this team, not live audio or a recording.</summary>
    /// <param name="context">The checked team, owner and bounded sources.</param>
    /// <param name="meetingId">A meeting verified against this team's stored session/calendar.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Cited evidence only; review proposals still require the existing approval guard.</returns>
    Task<Result<IReadOnlyList<WorkSignalEvidence>>> GetMeetingNotesAsync(WorkSignalContext context, string meetingId, CancellationToken ct);
}
