namespace Mcm.Scrum.Domain.Planning;

/// <summary>Where a planned item comes from.</summary>
public enum PlanningSource
{
    /// <summary>Unfinished work started in the current sprint; it is selected first.</summary>
    CarryOver,

    /// <summary>A ready item from the product backlog, taken in backlog order.</summary>
    Backlog,
}

/// <summary>How the planned points compare with the velocity range (Rajeev: "don't over promise, and don't under commit").</summary>
public enum CommitmentLevel
{
    /// <summary>Inside the velocity range.</summary>
    Within,

    /// <summary>Above the top of the range: over-promising.</summary>
    Over,

    /// <summary>Below the bottom of the range: under-committing.</summary>
    Under,
}
