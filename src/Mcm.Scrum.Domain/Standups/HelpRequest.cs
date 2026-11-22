namespace Mcm.Scrum.Domain.Standups;

/// <summary>A blocked teammate's item that this member was named as able to help with (M9-R2).</summary>
/// <param name="WorkItemId">The blocked item.</param>
/// <param name="Type">Its work item type.</param>
/// <param name="Title">Its title.</param>
/// <param name="BlockedMemberName">Who is blocked.</param>
public sealed record HelpRequest(int WorkItemId, string Type, string Title, string BlockedMemberName);
