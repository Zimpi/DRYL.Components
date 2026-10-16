namespace DRYL.Components;

/// <summary>
/// Visual AI state for any DRYL component that opts into AI mode. Drives the ambient
/// border, glow, and reveal animations so an AI-driven surface reads instantly as such.
/// </summary>
/// <remarks>
/// Pair this with tool-calling flows (e.g. <c>Microsoft.Extensions.AI</c>): set
/// <see cref="Thinking"/> when a tool call dispatches, <see cref="Streaming"/> while
/// tokens arrive, and <see cref="Generated"/> for a brief moment after completion
/// before settling on <see cref="Active"/> (or <see cref="None"/> if the surface is
/// only AI-driven transiently).
/// </remarks>
public enum AiState
{
    /// <summary>No AI styling — component renders normally.</summary>
    None,

    /// <summary>Persistent AI mode — slow rotating gradient border, breathing glow.</summary>
    Active,

    /// <summary>Tool call in flight — fast pulse on border and glow.</summary>
    Thinking,

    /// <summary>Tokens streaming in — moderate pulse, slightly faster than idle.</summary>
    Streaming,

    /// <summary>One-shot reveal after generation completes (lift + accent wash).</summary>
    Generated,

    /// <summary>
    /// The AI is done and a human now has to accept or dismiss what it produced — a still,
    /// frozen aurora edge that rests until the host changes the state. Accepting moves the
    /// surface to <see cref="Generated"/> (with a wave from the point of the click), dismissing
    /// moves it to <see cref="None"/>. Set it only where an accept or dismiss action exists.
    /// </summary>
    Proposed
}
