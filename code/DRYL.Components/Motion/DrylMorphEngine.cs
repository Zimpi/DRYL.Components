using Microsoft.JSInterop;

namespace DRYL.Components.Motion;

/// <inheritdoc cref="IDrylMorph"/>
internal sealed class DrylMorphEngine : IDrylMorph, IDisposable
{
    // The move itself and the arrival of a target that has no counterpart. Both are
    // token durations in the stylesheet; they are passed to JS rather than read there
    // so the two sides cannot drift.
    private const int MoveMs = 420;    // --dur-slow
    private const int EnterMs = 240;   // --dur-med

    private readonly IJSRuntime _js;
    private TaskCompletionSource? _renderTcs;

    // The navigation path latches instead of awaiting an event: the destination page can
    // render before this service gets a chance to look, and that render must not be lost.
    private bool _navRenderSeen;
    private TaskCompletionSource? _navRenderTcs;

    public DrylMorphEngine(IJSRuntime js) => _js = js;

    public Task RunAsync(Action mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);
        return RunAsync(() => { mutate(); return Task.CompletedTask; });
    }

    public async Task RunAsync(Func<Task> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        // First: where is everything now?
        var measured = await TryCaptureAsync();

        _renderTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await mutate();

        if (!measured) { _renderTcs = null; return; }

        // The caller's StateHasChanged() has queued a render; the new geometry only
        // exists once it has reached the DOM.
        await _renderTcs.Task;
        _renderTcs = null;

        // Last / Invert / Play.
        await TryPlayAsync();
    }

    public void SignalRendered()
    {
        _renderTcs?.TrySetResult();
        _navRenderSeen = true;
        _navRenderTcs?.TrySetResult();
    }

    public async Task BeginNavigationAsync(TimeSpan timeout)
    {
        _navRenderSeen = false;
        _navRenderTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!await TryCaptureAsync()) return;

        // Not awaited by the caller: a navigation must never wait on the morph.
        if (!_navRenderSeen)
            await Task.WhenAny(_navRenderTcs.Task, Task.Delay(timeout));

        await TryPlayAsync();
    }

    private async Task<bool> TryCaptureAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("dryl.morph.capture");
            return true;
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException)
        {
            // Prerender, a stale dryl.js, or a circuit that has gone away. The state
            // change still happens; it simply happens without a morph.
            return false;
        }
    }

    private async Task TryPlayAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("dryl.morph.play", MoveMs, EnterMs);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException)
        {
            // Same fallbacks as above — never let a missing morph break the state change.
        }
    }

    public void Dispose()
    {
        // Unblock anything waiting on a render that will now never come.
        _renderTcs?.TrySetResult();
        _navRenderTcs?.TrySetResult();
    }
}
