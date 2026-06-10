using Microsoft.AspNetCore.Components;

namespace DRYL.Components;

/// <summary>
/// Base class for AI-aware components that are not already bound to another base
/// (e.g. <c>InputBase&lt;T&gt;</c>). Provides the opt-in <see cref="Ai"/> parameter and
/// resolves it against a surrounding <c>DrylAiScope</c> via <see cref="EffectiveAi"/>.
/// </summary>
/// <remarks>
/// Inherit with <c>@inherits DrylAiAware</c> and drive the <c>.ai-aura*</c> classes from
/// <see cref="EffectiveAi"/> instead of <see cref="Ai"/>. Components that cannot change
/// their base (the <c>InputBase&lt;T&gt;</c> family) replicate the same two members inline:
/// a <c>[CascadingParameter] DRYL.Components.Ai.AiScope?</c> and an <c>EffectiveAi</c>
/// property delegating to <see cref="DRYL.Components.Ai.AiScope.Resolve"/>.
/// </remarks>
public abstract class DrylAiAware : ComponentBase
{
    /// <summary>
    /// AI ambient state for this component. Off by default. An explicit value always wins
    /// over a surrounding <c>DrylAiScope</c>.
    /// </summary>
    [Parameter] public AiState Ai { get; set; } = AiState.None;

    /// <summary>The surrounding AI scope, if any. Supplied by <c>DrylAiScope</c>.</summary>
    [CascadingParameter] protected DRYL.Components.Ai.AiScope? Scope { get; set; }

    /// <summary>
    /// The state to render: explicit <see cref="Ai"/> when set, otherwise the inherited
    /// <see cref="Scope"/> state, otherwise <see cref="AiState.None"/>.
    /// </summary>
    protected AiState EffectiveAi => DRYL.Components.Ai.AiScope.Resolve(Ai, Scope);
}
