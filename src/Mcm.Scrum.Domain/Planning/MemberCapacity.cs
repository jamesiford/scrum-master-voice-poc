namespace Mcm.Scrum.Domain.Planning;

/// <summary>One member's capacity for the next sprint and what is planned on them.</summary>
/// <param name="MemberId">Member id.</param>
/// <param name="DisplayName">Name.</param>
/// <param name="AvailabilityPct">Availability, 0 to 100.</param>
/// <param name="CapacityLow">Their share of the low end of the velocity range, times availability.</param>
/// <param name="CapacityHigh">Their share of the high end of the velocity range, times availability.</param>
/// <param name="PlannedPoints">Points of the selected items they own or are suggested to own.</param>
public sealed record MemberCapacity(string MemberId, string DisplayName, int AvailabilityPct, double CapacityLow, double CapacityHigh, double PlannedPoints)
{
    /// <summary>Gets a value indicating whether more is planned on them than their capacity allows.</summary>
    public bool Overloaded => PlannedPoints > CapacityHigh;
}
