namespace Mcm.Scrum.Domain.Standups;

/// <summary>Which ceremony a session runs. The aggregate is still named <c>StandupSession</c> (naming debt, ADR 0009).</summary>
public enum CeremonyKind
{
    /// <summary>The daily standup. The default, so sessions saved before ceremonies existed load unchanged.</summary>
    DailyScrum,

    /// <summary>Sprint planning.</summary>
    SprintPlanning,

    /// <summary>Sprint review.</summary>
    SprintReview,

    /// <summary>Sprint retrospective.</summary>
    Retrospective,
}

/// <summary>Plain-language names for a <see cref="CeremonyKind"/>.</summary>
public static class CeremonyKindNames
{
    /// <summary>Gets the lower-case phrase used in attribution and cards, for example "sprint planning".</summary>
    /// <param name="kind">The ceremony.</param>
    /// <returns>The phrase. The daily scrum is "standup" so existing attribution text is unchanged.</returns>
    public static string Phrase(this CeremonyKind kind) => kind switch
    {
        CeremonyKind.SprintPlanning => "sprint planning",
        CeremonyKind.SprintReview => "sprint review",
        CeremonyKind.Retrospective => "retrospective",
        _ => "standup",
    };
}
