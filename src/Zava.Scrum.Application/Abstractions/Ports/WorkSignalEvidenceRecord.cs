namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Team- and owner-scoped short reference record; no raw responses, credentials or conversation history.</summary>
/// <param name="TeamId">The application team.</param>
/// <param name="OwnerObjectId">The connection under which the source was read.</param>
/// <param name="CorrelationId">The ceremony/request correlation.</param>
/// <param name="RetainUntil">Deletion deadline derived from configured retention.</param>
/// <param name="Evidence">Only the verified link, source metadata and bounded excerpt.</param>
public sealed record WorkSignalEvidenceRecord(
    string TeamId, Guid OwnerObjectId, Guid CorrelationId, DateTimeOffset RetainUntil, WorkSignalEvidence Evidence);
