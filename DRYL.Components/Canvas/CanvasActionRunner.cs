using DRYL.Components.Dialogs;
using DRYL.Components.Toasts;
using Microsoft.Extensions.Logging;

namespace DRYL.Components.Canvas;

/// <summary>What one action button currently has to show.</summary>
public sealed class CanvasActionState
{
    internal CanvasActionState(bool busy, string? error)
    {
        Busy = busy;
        Error = error;
    }

    /// <summary>The handler is running — the button is in its loading beat.</summary>
    public bool Busy { get; }

    /// <summary>The last failure, shown inline under the button. It stays until the next attempt:
    /// a failure asks the user to do something, so it must not expire on its own.</summary>
    public string? Error { get; }
}

/// <summary>
/// Runs one canvas's <c>action</c> bindings. Owned by a <c>DrylCanvas</c> instance, like
/// <see cref="CanvasDataBinder"/> — two canvases on a page share nothing.
///
/// <para>Its only entry point is <see cref="InvokeAsync"/>, and its only caller is a rendered
/// button's click handler. There is deliberately no path from a model output to here: the AI
/// builds and labels the button, the human presses it.</para>
///
/// <para>A completed action is one movement: patch ops land in one batch and pulse, the named
/// sources reload through the existing binder, a success message is a toast, and a failure stays
/// inline at the button.</para>
///
/// <para><b>Never <c>ConfigureAwait(false)</c> in here.</b> The whole sequence starts on a click
/// and ends by touching components — the toast provider, the host's <c>OnAction</c>, the canvas's
/// own render. Dropping the Blazor dispatcher at any await means the continuation calls
/// <c>StateHasChanged</c> off-thread and kills the circuit. (<see cref="CanvasDataBinder"/> may do
/// it because every signal it raises is marshalled back through <c>InvokeAsync</c>; this runner
/// talks to components directly.)</para>
/// </summary>
public sealed class CanvasActionRunner
{
    private readonly ICanvasActionService _actions;
    private readonly ICanvasDataService? _data;
    private readonly CanvasFormState _form;
    private readonly IServiceProvider _services;
    private readonly ILogger? _log;

    private readonly Dictionary<string, CanvasActionState> _states = new(StringComparer.Ordinal);

    /// <summary>Creates a runner for one canvas.</summary>
    /// <param name="actions">The scope's action service.</param>
    /// <param name="data">The scope's data service, or <c>null</c> when no sources are registered —
    /// a result's <c>Refresh</c> list is then a no-op.</param>
    /// <param name="form">The canvas's live field values, for <c>$field</c> arguments.</param>
    /// <param name="services">The scope, used to reach the dialog and toast services lazily.</param>
    /// <param name="log">Optional logger for handler exceptions and skipped ops.</param>
    public CanvasActionRunner(ICanvasActionService actions, ICanvasDataService? data,
                              CanvasFormState form, IServiceProvider services, ILogger? log = null)
    {
        _actions = actions ?? throw new ArgumentNullException(nameof(actions));
        _data = data;
        _form = form ?? throw new ArgumentNullException(nameof(form));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log;
    }

    /// <summary>Applies a patch op from an action result. Set by <c>DrylCanvas</c>, which owns the
    /// spec; returns <c>null</c> on success or a skip reason.</summary>
    public Func<CanvasOp, string?>? Patch { get; set; }

    /// <summary>Invoked for a result that called <see cref="CanvasActionResult.AskAi"/>.
    /// <c>DrylCanvas</c> forwards it to <c>OnInteraction</c>, so an existing chat wiring picks it
    /// up unchanged.</summary>
    public Func<CanvasInteraction, Task>? Ask { get; set; }

    /// <summary>Invoked after every completed action, successful or not.</summary>
    public Func<CanvasActionOutcome, Task>? Completed { get; set; }

    /// <summary>Raised whenever a button's busy/error state changed. The canvas re-renders.</summary>
    public event Action? OnChanged;

    /// <summary>What <paramref name="nodeId"/> should render, or <c>null</c> if it has never run.</summary>
    public CanvasActionState? StateOf(string nodeId) =>
        _states.TryGetValue(nodeId, out var state) ? state : null;

    /// <summary>
    /// Runs the action bound to <paramref name="nodeId"/>. <paramref name="label"/> is the button's
    /// visible label and titles the confirmation dialog. Never throws.
    /// </summary>
    public async Task InvokeAsync(string nodeId, string? label, CanvasActionBinding action)
    {
        if (string.IsNullOrWhiteSpace(action.Name)) return;
        if (StateOf(nodeId)?.Busy == true) return;      // a second press while the first still runs

        var name = action.Name!;
        var args = CanvasArgs.Resolve(action.Args, _form, out _);
        var values = _form.Snapshot();

        if (!string.IsNullOrWhiteSpace(action.Confirm))
        {
            var decision = await ConfirmAsync(nodeId, label, action.Confirm!);
            if (decision is not true) return;           // declined, or refused for lack of a dialog
        }

        Set(nodeId, new CanvasActionState(busy: true, error: null));

        CanvasActionResult result;
        try
        {
            result = await _actions.InvokeAsync(name, args, nodeId, values, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // A handler that throws must never reach the renderer, let alone the circuit — and the
            // user gets the action's name, not the exception's innards.
            _log?.LogError(ex, "Canvas action '{Action}' failed.", name);
            var failure = $"Action '{name}' failed.";
            Set(nodeId, new CanvasActionState(busy: false, error: failure));
            await NotifyAsync(nodeId, name, false, failure);
            return;
        }

        if (result.Succeeded) ApplyResult(result);
        Set(nodeId, new CanvasActionState(busy: false, error: result.Succeeded ? null : result.Message));

        if (result.Succeeded && result.Ask is { } ask && Ask is not null)
            await Ask(new CanvasInteraction(name, nodeId, values) { Message = ask });

        await NotifyAsync(nodeId, name, result.Succeeded, result.Message);
    }

    // Ops first (the instant visual), then the data catch-up, then the toast: the user sees the
    // artifact move before being told that it moved.
    private void ApplyResult(CanvasActionResult result)
    {
        if (Patch is { } patch)
        {
            foreach (var op in result.Ops)
                if (patch(op) is { } reason)
                    _log?.LogWarning("Canvas action patch op skipped: {Reason}", reason);
        }

        if (_data is not null)
        {
            foreach (var notice in result.Refreshes) _data.Invalidate(notice);
        }

        if (result.Message is { Length: > 0 } message) Toasts?.ShowSuccess(message);
    }

    private async Task NotifyAsync(string nodeId, string name, bool ok, string? message)
    {
        if (Completed is { } completed)
            await completed(new CanvasActionOutcome(name, nodeId, ok, message));
    }

    // null = refused (no dialog service), false = declined, true = go ahead.
    private async Task<bool?> ConfirmAsync(string nodeId, string? label, string question)
    {
        if (Dialogs is not { } dialogs)
        {
            // An action the author deliberately gated behind a confirmation must never run
            // unconfirmed just because the host forgot the provider.
            _log?.LogError("A canvas action requires confirmation but no IDrylDialogService is available.");
            Set(nodeId, new CanvasActionState(false, "Confirmation is unavailable — the action was not run."));
            return null;
        }

        var title = string.IsNullOrWhiteSpace(label) ? "Confirm" : label!;
        var result = await dialogs.ShowConfirmAsync(title, question, confirmLabel: title);
        return !result.Canceled;
    }

    private void Set(string nodeId, CanvasActionState state)
    {
        _states[nodeId] = state;
        OnChanged?.Invoke();
    }

    // Resolved lazily: a canvas that never hosts an action must not force the host to register
    // either provider, and both are scoped services the runner does not own.
    private IDrylDialogService? Dialogs =>
        _services.GetService(typeof(IDrylDialogService)) as IDrylDialogService;

    private IDrylToastService? Toasts =>
        _services.GetService(typeof(IDrylToastService)) as IDrylToastService;
}
