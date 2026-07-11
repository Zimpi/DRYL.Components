using Microsoft.JSInterop;

namespace DRYL.Components.Motion;

/// <inheritdoc cref="IDrylViewTransition"/>
internal sealed class DrylViewTransition : IDrylViewTransition, IDisposable
{
    private readonly IJSRuntime _js;
    private readonly DotNetObjectReference<DrylViewTransition> _selfRef;
    private TaskCompletionSource? _renderTcs;
    private Func<Task>? _pending;

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

    /// <summary>Invoked from JS inside <c>document.startViewTransition</c>'s update
    /// callback (or directly on the fallback path). Applies the pending mutation and
    /// resolves once the consuming component reports the render reached the DOM.</summary>
    [JSInvokable]
    public async Task ApplyChange()
    {
        var pending = Interlocked.Exchange(ref _pending, null);
        if (pending is null) return; // already applied — nothing to snapshot
        _renderTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await pending();
        // The caller's StateHasChanged() (inside mutate) has queued a render;
        // resolve only once it actually reaches the DOM (SignalRendered).
        await _renderTcs.Task;
    }

    public void SignalRendered() => _renderTcs?.TrySetResult();

    public void Dispose()
    {
        _renderTcs?.TrySetResult(); // unblock an in-flight ApplyChange
        _selfRef.Dispose();
    }
}
