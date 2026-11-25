namespace Zava.Scrum.Domain.Identity;

/// <summary>Where an actor's identity was established.</summary>
public enum ActorSource
{
    /// <summary>A Teams activity, authenticated by the Bot Service token (<c>Activity.From.AadObjectId</c>).</summary>
    Teams,

    /// <summary>The admin portal, authenticated by an Entra bearer token validated in the API.</summary>
    Portal,

    /// <summary>Microsoft 365 Copilot. Not built; reserved so the model does not change when it is.</summary>
    Copilot,

    /// <summary>The agent itself (a managed identity); it proposes, it never approves.</summary>
    Agent,
}
