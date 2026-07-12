namespace DRYL.Components.Ai;

/// <summary>
/// Immutable context cascaded by <c>DrylAiScope</c> to its descendants so that
/// AI-aware components inherit a shared <see cref="AiState"/> keyed by operation.
/// </summary>
/// <remarks>
/// Consumers never construct this directly — place a <c>&lt;DrylAiScope/&gt;</c> in
/// the tree and AI-aware components inside it pick the state up automatically. The
/// resolution rule (explicit <c>Ai</c> wins, otherwise inherit the scope) lives in
/// <see cref="Resolve(AiState, AiScope?)"/> so every consumer applies it identically.
/// </remarks>
public sealed class AiScope
{
    /// <summary>The operation key this scope tracks, or <c>null</c> for an explicit, service-less scope.</summary>
    public string? Key { get; init; }

    /// <summary>The current AI state broadcast to descendants.</summary>
    public AiState State { get; init; }

    /// <summary>
    /// The aura variant broadcast to descendants, or <c>null</c> when the scope does not
    /// pin one (descendants then fall back to <see cref="AiAura.Comet"/>).
    /// </summary>
    public AiAura? Aura { get; init; }

    /// <summary>
    /// Resolve the effective state for a component. An explicit
    /// <paramref name="explicitAi"/> (anything other than <see cref="AiState.None"/>)
    /// always wins; otherwise the surrounding <paramref name="scope"/>'s state is
    /// inherited. Falls back to <see cref="AiState.None"/> when there is no scope.
    /// </summary>
    public static AiState Resolve(AiState explicitAi, AiScope? scope) =>
        explicitAi != AiState.None ? explicitAi : (scope?.State ?? AiState.None);

    /// <summary>
    /// Resolve the effective aura variant for a component. An explicit
    /// <paramref name="explicitAura"/> always wins; otherwise the surrounding
    /// <paramref name="scope"/>'s variant is inherited; otherwise defaults to
    /// <see cref="AiAura.Comet"/>. Unlike <see cref="Resolve(AiState, AiScope?)"/> there is
    /// no "off" sentinel — <c>Comet</c> is a real default, so the opt-out is <c>null</c>.
    /// </summary>
    public static AiAura ResolveAura(AiAura? explicitAura, AiScope? scope) =>
        explicitAura ?? scope?.Aura ?? AiAura.Comet;
}
