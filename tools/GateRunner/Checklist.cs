namespace Zava.Scrum.Tools.GateRunner;

/// <summary>Prints the Teams rehearsal checklist (<c>docs/gates/teams-rehearsal.md</c>) for a person to tick after the automated run.</summary>
internal static class Checklist
{
    /// <summary>Finds and prints the checklist.</summary>
    /// <returns><see langword="true"/> when the file was found.</returns>
    public static bool Print()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var file = Path.Combine(dir.FullName, "docs", "gates", "teams-rehearsal.md");
            if (File.Exists(file))
            {
                Console.WriteLine();
                Console.WriteLine(File.ReadAllText(file));
                return true;
            }
        }

        Console.WriteLine("teams-rehearsal.md not found; run from inside the repository.");
        return false;
    }
}
