namespace Mcm.Scrum.Domain.Planning;

/// <summary>Where a planning pack is in its life.</summary>
public enum PlanningPackStatus
{
    /// <summary>Prepared and waiting for the Scrum Master.</summary>
    Prepared,

    /// <summary>A person approved a selection; its changes were handed to the approval-gated writer.</summary>
    Approved,

    /// <summary>A person rejected the plan. Nothing was written.</summary>
    Rejected,
}
