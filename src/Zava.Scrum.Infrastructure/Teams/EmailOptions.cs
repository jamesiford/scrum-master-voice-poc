using System.ComponentModel.DataAnnotations;

namespace Zava.Scrum.Infrastructure.Teams;

/// <summary>Binds <c>Email__*</c>: how follow-up emails are delivered (M9-R1). <c>Features:EmailFollowUps</c> still switches the feature on or off.</summary>
public sealed class EmailOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Email";

    /// <summary>Gets or sets the delivery mode: <c>Outbox</c> records the email only; <c>Graph</c> also sends it through Microsoft Graph.</summary>
    [RegularExpression("^(Outbox|Graph)$", ErrorMessage = "Email:Mode must be Outbox or Graph.")]
    public string Mode { get; set; } = "Outbox";

    /// <summary>Gets or sets the mailbox the agent sends from (user principal name or address in the demo tenant). Required for <c>Graph</c>.</summary>
    public string? Sender { get; set; }

    /// <summary>Gets a value indicating whether mail is sent through Graph.</summary>
    public bool UsesGraph => string.Equals(Mode, "Graph", StringComparison.OrdinalIgnoreCase);
}
