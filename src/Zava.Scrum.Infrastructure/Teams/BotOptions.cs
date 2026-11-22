namespace Zava.Scrum.Infrastructure.Teams;

/// <summary>Binds <c>Bot__*</c> configuration (SPEC 17). Proactive Teams delivery is off until <see cref="AppId"/> is set.</summary>
public sealed class BotOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Bot";

    /// <summary>Gets or sets the bot's Microsoft Entra application (client) id.</summary>
    public string? AppId { get; set; }

    /// <summary>Gets a value indicating whether the bot is configured.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(AppId);
}
