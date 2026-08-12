namespace DRYL.Components;

/// <summary>
/// Visual variant of the AI aura, orthogonal to <see cref="AiState"/>. Both variants
/// share the same state semantics, colour language and one-shot reveal — they differ
/// only in how the ambient "life" is expressed on the surface.
/// </summary>
/// <remarks>
/// Set via the shared <c>Aura</c> parameter on any AI-aware component, or once on a
/// surrounding <c>DrylAiScope</c> to switch a whole subtree. Leaving it unset inherits
/// the scope, ultimately falling back to <see cref="Comet"/>.
/// </remarks>
public enum AiAura
{
    /// <summary>
    /// Default. Bold and eye-catching: an even hairline saum plus a bright travelling
    /// light ("comet") whose motion character changes per <see cref="AiState"/>. Best for
    /// one or two prominent AI surfaces on a page.
    /// </summary>
    Comet,

    /// <summary>
    /// Calmer alternative: a soft, blurred, flowing edge field instead of a travelling
    /// point. Reads well when many AI-aware surfaces are visible at once, so a dense page
    /// does not feel overloaded. Same states and colour weighting as <see cref="Comet"/>.
    /// </summary>
    Aurora
}
