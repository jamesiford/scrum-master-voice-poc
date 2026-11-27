namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>The only source types permitted for team signals; mail and private chats are intentionally absent.</summary>
public enum WorkSignalSource
{
    /// <summary>A message in the team's explicitly selected channel.</summary>
    Channel,

    /// <summary>Existing notes or transcript excerpts from a team-owned meeting.</summary>
    Meeting,
}
