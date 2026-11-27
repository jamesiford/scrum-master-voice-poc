namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>A short, source-labelled excerpt. Metadata must be verified before this reaches a card or model.</summary>
/// <param name="Source">The source type; only team channel messages and stored meeting notes are allowed.</param>
/// <param name="Title">The source's title, not an invented model title.</param>
/// <param name="Link">The HTTPS source link, reauthorised when followed.</param>
/// <param name="Timestamp">The source's timestamp.</param>
/// <param name="Snippet">A bounded excerpt, not full messages or transcripts.</param>
/// <param name="ResourceId">The exact channel or meeting id from structured source metadata.</param>
/// <param name="MemberObjectId">The source author's verified roster identity, if available.</param>
/// <param name="TeamsGroupId">Structured group metadata required for channel messages.</param>
public sealed record WorkSignalEvidence(
    WorkSignalSource Source, string Title, Uri Link, DateTimeOffset Timestamp,
    string Snippet, string ResourceId, Guid? MemberObjectId = null, string? TeamsGroupId = null);
