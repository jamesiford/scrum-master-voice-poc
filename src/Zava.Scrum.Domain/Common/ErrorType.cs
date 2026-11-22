namespace Mcm.Scrum.Domain.Common;

/// <summary>Classifies an <see cref="Error"/> so adapters can map it to a transport status without parsing codes.</summary>
public enum ErrorType
{
    /// <summary>The request was malformed or failed validation.</summary>
    Validation,

    /// <summary>A referenced entity does not exist.</summary>
    NotFound,

    /// <summary>The request conflicts with current state.</summary>
    Conflict,

    /// <summary>The action requires an approved follow-up that does not exist (customer requirement F7).</summary>
    NotApproved,

    /// <summary>A downstream system (for example Azure DevOps) rejected the operation.</summary>
    ExternalRejected,

    /// <summary>A bug or infrastructure fault.</summary>
    Unexpected,
}
