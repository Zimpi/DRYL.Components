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
    /// Resolve the effective state for a component. An explicit
    /// <paramref name="explicitAi"/> (anything other than <see cref="AiState.None"/>)
    /// always wins; otherwise the surrounding <paramref name="scope"/>'s state is
    /// inherited. Falls back to <see cref="AiState.None"/> when there is no scope.
    /// </summary>
    public static AiState Resolve(AiState explicitAi, AiScope? scope) =>
        explicitAi != AiState.None ? explicitAi : (scope?.State ?? AiState.None);
}
