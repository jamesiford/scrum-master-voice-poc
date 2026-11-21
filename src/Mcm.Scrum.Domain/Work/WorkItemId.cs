namespace Mcm.Scrum.Domain.Work;

/// <summary>Azure DevOps work item id.</summary>
/// <param name="Value">The numeric id.</param>
public readonly record struct WorkItemId(int Value)
{
    /// <inheritdoc />
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
