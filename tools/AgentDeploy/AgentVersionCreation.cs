using System.ClientModel;

namespace Zava.Scrum.Tools.AgentDeploy;

/// <summary>Recovers the Foundry workspace identity conflict without retrying other deployment failures.</summary>
internal static class AgentVersionCreation
{
    /// <summary>Creates a version, reading the current agent before at most two conflict retries.</summary>
    /// <typeparam name="T">The version returned by the tool's isolated preview SDK.</typeparam>
    /// <param name="createVersion">Creates the real hosted version.</param>
    /// <param name="readCurrentAgent">Refreshes the current agent as directed by the service.</param>
    /// <returns>The successfully created version.</returns>
    internal static T Create<T>(Func<T> createVersion, Action readCurrentAgent)
    {
        for (var retry = 0; ; retry++)
        {
            try
            {
                return createVersion();
            }
            catch (ClientResultException exception) when (
                retry < 2 && exception.Status == 409 &&
                exception.Message.Contains("project workspace identity changed during version creation", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine($"Foundry workspace identity changed; reading current agent before creation retry {retry + 1}/2.");
                readCurrentAgent();
            }
        }
    }
}
