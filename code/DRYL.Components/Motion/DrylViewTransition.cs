using Microsoft.JSInterop;

namespace DRYL.Components.Motion;

/// <inheritdoc cref="IDrylViewTransition"/>
internal sealed class DrylViewTransition : IDrylViewTransition, IDisposable
{
    private readonly IJSRuntime _js;
    private readonly DotNetObjectReference<DrylViewTransition> _selfRef;
    private TaskCompletionSource? _renderTcs;
    private Func<Task>? _pending;

    // The navigation path (BeginNavigation). It has no mutate delegate — the Router
    // does the mutating — so ApplyChange only waits for the destination page to
    // render. _navRenderSeen is a latch rather than an event: the destination can
    // render before JS gets round to calling ApplyChange, and that render must not
    // be lost, or the old frame would be held until the timeout for no reason.
    private bool _navPending;
    private bool _navRenderSeen;
    private TimeSpan _navTimeout;
    private TaskCompletionSource? _navRenderTcs;

    public DrylViewTransition(IJSRuntime js)
    {
        _js = js;
        _selfRef = DotNetObjectReference.Create(this);
    }

    public Task RunAsync(Action mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);
        return RunAsync(() => { mutate(); return Task.CompletedTask; });
    }

    public async Task RunAsync(Func<Task> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);
        _pending = mutate;
        try
        {
            await _js.InvokeVoidAsync("dryl.viewTransition.start", _selfRef);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            // Circuit gone, stale dryl.js without the module, or prerender
            // (no JS yet) — fall through to the direct apply below.
        }
        // JS guarantees ApplyChange ran before its promise resolves. If it never
        // came (prerender, disconnected circuit, non-browser renderer), the state
        // change must still happen — apply it directly, morph-free.
        if (Interlocked.Exchange(ref _pending, null) is { } missed) await missed();
    }

    /// <inheritdoc cref="IDrylViewTransition.BeginNavigation"/>
    public void BeginNavigation(TimeSpan timeout)
    {
        _navTimeout = timeout;
        _navRenderSeen = false;
        _navRenderTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _navPending = true;
        // Deliberately not awaited: the navigation must not wait for the morph, and
        // awaiting here would deadlock — the transition waits for the new page's
        // render, which cannot happen until the location-changing handler returns.
        _ = StartForNavigationAsync();
    }

    private async Task StartForNavigationAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("dryl.viewTransition.start", _selfRef);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            // Circuit gone, stale dryl.js, or prerender — the navigation itself is
            // unaffected; it simply happens without a morph.
        }
        finally
        {
            _navPending = false;
        }
    }

    /// <summary>Invoked from JS inside <c>document.startViewTransition</c>'s update
    /// callback (or directly on the fallback path). Applies the pending mutation and
    /// resolves once the consuming component reports the render reached the DOM.</summary>
    [JSInvokable]
    public async Task ApplyChange()
    {
        // Navigation path: there is nothing to mutate, only a destination to wait for.
        if (_navPending)
        {
            _navPending = false;
            if (_navRenderSeen) return; // already rendered — take the new snapshot now
            if (_navRenderTcs is { } tcs)
            {
                // The bail: a destination carrying no DrylMorph, or one that never
                // finishes rendering, must not leave the user looking at a held frame.
                await Task.WhenAny(tcs.Task, Task.Delay(_navTimeout));
            }
            return;
        }

        var pending = Interlocked.Exchange(ref _pending, null);
        if (pending is null) return; // already applied — nothing to snapshot
        _renderTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await pending();
        // The caller's StateHasChanged() (inside mutate) has queued a render;
        // resolve only once it actually reaches the DOM (SignalRendered).
        await _renderTcs.Task;
    }

    public void SignalRendered()
    {
        _renderTcs?.TrySetResult();
        // Latch, not an event: this render may arrive before JS calls ApplyChange.
        _navRenderSeen = true;
        _navRenderTcs?.TrySetResult();
    }

    public void Dispose()
    {
        _renderTcs?.TrySetResult(); // unblock an in-flight ApplyChange
        _navRenderTcs?.TrySetResult();
        _selfRef.Dispose();
    }
}
