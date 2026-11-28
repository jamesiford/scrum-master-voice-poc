using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.Demo.ReseedDemo;

/// <summary>Reports a reset concurrency refusal from void persistence ports; the mediator maps it to an explicit 409, never a silent write or unexpected 500.</summary>
public sealed class DemoResetConflictException : InvalidOperationException
{
    /// <summary>Creates a conflict describing the refused persistence operation.</summary>
    public DemoResetConflictException(string message, string code = "demo.reset_in_progress") : base(message) =>
        Error = Zava.Scrum.Domain.Common.Error.Conflict(code, message);

    /// <summary>Gets the typed conflict returned by the application pipeline.</summary>
    public Error Error { get; }
}
