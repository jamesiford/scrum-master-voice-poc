namespace Zava.Scrum.Domain.Voice;

/// <summary>What kind of statement a member made about a work item (third step of the utterance analysis).</summary>
public enum CommentKind
{
    /// <summary>Work done or under way, with nothing to track.</summary>
    Progress,

    /// <summary>The item is finished.</summary>
    Done,

    /// <summary>Something stops the item moving.</summary>
    Blocker,

    /// <summary>The finish or target date moves.</summary>
    DateChange,

    /// <summary>The member says what they will do next.</summary>
    Commitment,

    /// <summary>Something explicitly decided.</summary>
    Decision,

    /// <summary>The remaining work or effort changes.</summary>
    Estimate,
}
