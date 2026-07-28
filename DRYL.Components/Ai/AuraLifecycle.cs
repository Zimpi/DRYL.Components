namespace DRYL.Components.Ai;

/// <summary>
/// Drives the mount lifecycle of a component's AI aura so it animates <em>out</em> as
/// well as in. Without this, dropping <see cref="AiState"/> to <see cref="AiState.None"/>
/// yanks the <c>.ai-aura-*</c> elements from the DOM instantly and the border/glow flicker
/// away (violating the enter-and-exit rule). A host composes one instance, calls
/// <see cref="Sync"/> from <c>OnParametersSet</c>, and renders the aura while
/// <see cref="Present"/> is true — adding the <c>ai-aura--out</c> class while
/// <see cref="Exiting"/> so CSS can dissolve it over one <c>--dur-slow</c> beat.
/// </summary>
/// <remarks>
/// Reusable across every AI-aware host, whether it derives from <see cref="DrylAiAware"/>
/// or replicates the members inline (the <c>InputBase&lt;T&gt;</c> family). Prerender-safe:
/// it never touches JS, only <see cref="System.Threading.Tasks.Task.Delay(int, System.Threading.CancellationToken)"/>
/// plus the caller's re-render callback. Dispose it with the host.
/// </remarks>
public sealed class AuraLifecycle : IDisposable
{
    private readonly int _exitMs;
    private readonly int _generatedHoldMs;
    private CancellationTokenSource? _exitCts;
    private AiState _lastEffective = AiState.None;

    /// <param name="exitMs">
    /// How long the aura lingers, fading, after the state drops to None. Must be at least
    /// the CSS <c>--dur-slow</c> (420ms) so the dissolve completes before unmount.
    /// </param>
    /// <param name="generatedHoldMs">
    /// How long <see cref="AiState.Generated"/> stays on screen before retiring itself. Must
    /// outlast the CSS one-shot (the bloom ends at 900ms, the comet's retire at 1900ms).
    /// </param>
    public AuraLifecycle(int exitMs = 460, int generatedHoldMs = 1900)
    {
        _exitMs = exitMs;
        _generatedHoldMs = generatedHoldMs;
    }

    /// <summary>The state the aura should currently render (frozen at the last live state while exiting).</summary>
    public AiState RenderState { get; private set; } = AiState.None;

    /// <summary>True while the aura is fading out after leaving AI mode.</summary>
    public bool Exiting { get; private set; }

    /// <summary>True whenever the aura elements should be in the DOM — live or fading out.</summary>
    public bool Present => RenderState != AiState.None || Exiting;

    /// <summary>
    /// Reconcile against the host's effective state. Call from <c>OnParametersSet</c>.
    /// Re-entering AI mode cancels a pending exit; leaving it starts the fade and schedules
    /// the final unmount re-render via <paramref name="requestRender"/>.
    /// </summary>
    /// <param name="effective">The host's resolved <see cref="AiState"/> (e.g. <c>EffectiveAi</c>).</param>
    /// <param name="requestRender">
    /// Re-render callback, typically <c>() =&gt; InvokeAsync(StateHasChanged)</c>, invoked once
    /// after the exit window elapses so the host drops the aura from the DOM.
    /// </param>
    public void Sync(AiState effective, Func<Task> requestRender)
    {
        var entering = effective != _lastEffective;
        _lastEffective = effective;

        if (effective == AiState.Generated)
        {
            // Generated is a one-shot, not a resting place. It plays, then takes itself
            // off the surface — a host announces "done" and is finished, it never has to
            // remember to hand back to None. Until it retired, an aura that had nothing
            // left to say kept a breathing halo alive for the life of the page.
            if (entering)
            {
                _exitCts?.Cancel();
                Exiting = false;
                RenderState = AiState.Generated;
                _exitCts = new CancellationTokenSource();
                _ = RetireGeneratedAsync(_exitCts.Token, requestRender);
            }
            // Re-rendering while spent must not bring it back for another lap.
            return;
        }

        if (effective != AiState.None)
        {
            // Live (or re-activated mid-exit): cancel any pending exit and show it.
            _exitCts?.Cancel();
            _exitCts = null;
            Exiting = false;
            RenderState = effective;
        }
        else if (RenderState != AiState.None && !Exiting)
        {
            // Just left AI mode: keep the last look, start the dissolve, schedule unmount.
            Exiting = true;
            _exitCts?.Cancel();
            _exitCts = new CancellationTokenSource();
            _ = DelayExitAsync(_exitCts.Token, requestRender);
        }
    }

    private async Task RetireGeneratedAsync(CancellationToken token, Func<Task> requestRender)
    {
        try { await Task.Delay(_generatedHoldMs, token); }
        catch (TaskCanceledException) { return; }
        if (token.IsCancellationRequested) return;

        Exiting = true;
        await requestRender();

        try { await Task.Delay(_exitMs, token); }
        catch (TaskCanceledException) { return; }
        if (token.IsCancellationRequested) return;

        Exiting = false;
        RenderState = AiState.None;
        await requestRender();
    }

    private async Task DelayExitAsync(CancellationToken token, Func<Task> requestRender)
    {
        try { await Task.Delay(_exitMs, token); }
        catch (TaskCanceledException) { return; }
        if (token.IsCancellationRequested) return;

        Exiting = false;
        RenderState = AiState.None;
        await requestRender();
    }

    public void Dispose()
    {
        _exitCts?.Cancel();
        _exitCts = null;
    }
}
