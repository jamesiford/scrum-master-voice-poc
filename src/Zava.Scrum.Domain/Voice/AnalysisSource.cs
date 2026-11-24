namespace Zava.Scrum.Domain.Voice;

/// <summary>Where an <see cref="UtteranceAnalysis"/> came from.</summary>
public enum AnalysisSource
{
    /// <summary>Plain rules: the model was off, unavailable, too slow, unsure, or contradicted the roster.</summary>
    Rules,

    /// <summary>The decision model, after every id and name was checked in code.</summary>
    Model,
}
