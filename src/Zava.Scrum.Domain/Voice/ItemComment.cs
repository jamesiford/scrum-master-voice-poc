namespace Zava.Scrum.Domain.Voice;

/// <summary>One statement about one or more work items, classified by <see cref="CommentKind"/>.</summary>
/// <param name="Kind">What kind of statement it is.</param>
/// <param name="WorkItemIds">The sprint items it is about; empty when the member did not say which.</param>
/// <param name="Text">One short sentence, as recorded.</param>
/// <param name="NewDate">For a <see cref="CommentKind.DateChange"/>, the date as said ("Friday", "next sprint"), never parsed or written anywhere.</param>
public sealed record ItemComment(CommentKind Kind, IReadOnlyList<int> WorkItemIds, string Text, string? NewDate = null)
{
    /// <summary>Gets the code-owned calendar interpretation of a date note. This does not authorise or create an Azure DevOps date change.</summary>
    public SpokenDateResolution? DateResolution { get; init; }
}
