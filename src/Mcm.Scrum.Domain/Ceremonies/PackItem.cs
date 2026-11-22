using Mcm.Scrum.Domain.FollowUps;

namespace Mcm.Scrum.Domain.Ceremonies;

/// <summary>One line of a ceremony pack: what is proposed, and the change that is written only if the pack is approved.</summary>
/// <param name="Text">Readable description for the card.</param>
/// <param name="Change">The proposed Azure DevOps change.</param>
public sealed record PackItem(string Text, ProposedChange Change);
