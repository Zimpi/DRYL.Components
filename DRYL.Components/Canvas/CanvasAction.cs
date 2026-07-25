using System.Text.Json;

namespace DRYL.Components.Canvas;

/// <summary>
/// A node's link to a registered host action. Declared by the model as
/// <c>"action": { "name": "order.approve", "args": { … }, "confirm": "Really?" }</c>.
/// <para>The AI authors and labels the button; it never presses it. A write only ever happens
/// through a deliberate human action inside the artifact.</para>
/// </summary>
public sealed class CanvasActionBinding
{
    /// <summary>The registered action name, e.g. <c>order.approve</c>.</summary>
    public string? Name { get; set; }

    /// <summary>Argument object. Each value is a literal or a field reference
    /// <c>{ "$field": "&lt;name of an interactive node&gt;" }</c> — the same syntax a data
    /// binding uses for its params.</summary>
    public JsonElement? Args { get; set; }

    /// <summary>When set, the user must confirm this question in a dialog before the handler
    /// runs. Use it for anything destructive or irreversible.</summary>
    public string? Confirm { get; set; }
}

/// <summary>What an action handler gets besides its typed arguments.</summary>
public sealed class CanvasActionContext
{
    internal CanvasActionContext(IServiceProvider services, string nodeId,
                                 IReadOnlyDictionary<string, object?> values)
    {
        Services = services;
        NodeId = nodeId;
        Values = values;
    }

    /// <summary>
    /// The circuit's (or request's) service scope. The handler resolves its <c>DbContext</c>,
    /// <c>IHttpClientFactory</c> or the signed-in user from here — tenant and user never travel
    /// through the artifact spec, so the model cannot influence them.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>The id of the button the user pressed.</summary>
    public string NodeId { get; }

    /// <summary>Every interactive field of the artifact, not only those named in <c>args</c>.</summary>
    public IReadOnlyDictionary<string, object?> Values { get; }

    /// <summary>A field from <see cref="Values"/>, or <c>default</c> when absent or of another type.</summary>
    public T? Get<T>(string name) =>
        Values.TryGetValue(name, out var value) && value is T typed ? typed : default;
}

/// <summary>
/// What the canvas should do once an action handler returns: show a message, reload data, patch
/// the artifact, and optionally tell the assistant about it.
/// <para>A fluent, mutating builder — every method returns the same instance.</para>
/// </summary>
public sealed class CanvasActionResult
{
    private readonly List<CanvasInvalidation> _refreshes = new();
    private readonly List<CanvasOp> _ops = new();

    private CanvasActionResult(bool succeeded, string? message)
    {
        Succeeded = succeeded;
        Message = message;
    }

    /// <summary>The action succeeded. <paramref name="message"/> is shown as a success toast.</summary>
    public static CanvasActionResult Ok(string? message = null) => new(true, message);

    /// <summary>The action failed. <paramref name="message"/> is shown inline at the button and
    /// stays there until the next attempt — a failure asks the user to react, so it must not
    /// expire on its own.</summary>
    public static CanvasActionResult Fail(string message) => new(false, message);

    /// <summary>False when the handler reported a domain failure.</summary>
    public bool Succeeded { get; }

    /// <summary>The success toast text, or the inline failure text.</summary>
    public string? Message { get; }

    /// <summary>Data sources to reload once the action has run.</summary>
    public IReadOnlyList<CanvasInvalidation> Refreshes => _refreshes;

    /// <summary>Patch ops applied to the artifact in one batch — one action is one movement.</summary>
    public IReadOnlyList<CanvasOp> Ops => _ops;

    /// <summary>The message handed to the assistant afterwards, or <c>null</c> (the default).</summary>
    public string? Ask { get; private set; }

    /// <summary>Reloads every binding on these sources — in this canvas and in any other.</summary>
    public CanvasActionResult Refresh(params string[] sources)
    {
        foreach (var s in sources)
            if (!string.IsNullOrWhiteSpace(s)) _refreshes.Add(new CanvasInvalidation(s, null));
        return this;
    }

    /// <summary>Reloads only the bindings on <paramref name="source"/> whose params match
    /// <paramref name="parameters"/> (an anonymous object or the parameter record).</summary>
    public CanvasActionResult Refresh(string source, object parameters)
    {
        _refreshes.Add(new CanvasInvalidation(source,
            CanvasDataKey.Canonicalize(JsonSerializer.SerializeToElement(parameters, CanvasJson.Options))));
        return this;
    }

    /// <summary>Applies patch ops to the artifact — e.g. flipping a badge to green without
    /// waiting for a data source or an AI turn.</summary>
    public CanvasActionResult Patch(params CanvasOp[] ops)
    {
        foreach (var op in ops)
            if (op is not null) _ops.Add(op);
        return this;
    }

    /// <summary>Tells the assistant what happened, as the next chat turn. Off by default: the AI
    /// reacts to an action, it never causes one.</summary>
    public CanvasActionResult AskAi(string message)
    {
        Ask = message;
        return this;
    }
}

/// <summary>Everything the model, the validator and the runner need to know about one action —
/// and deliberately nothing more: never a row, never a personal record.</summary>
public sealed record CanvasActionDescriptor(
    string Name, string Description, IReadOnlyList<CanvasParamInfo> Args);

/// <summary>Reported to the host after every completed action — successful or not.</summary>
/// <param name="Action">The registered action name.</param>
/// <param name="NodeId">The button that ran it.</param>
/// <param name="Succeeded">False for a domain failure or a handler exception.</param>
/// <param name="Message">The success toast text, or the failure text shown at the button.</param>
public sealed record CanvasActionOutcome(
    string Action, string NodeId, bool Succeeded, string? Message);

/// <summary>A registered action: its descriptor plus the invoker that runs the host's handler.</summary>
public sealed class CanvasActionSource
{
    internal CanvasActionSource(
        CanvasActionDescriptor descriptor,
        Func<JsonElement?, CanvasActionContext, CancellationToken, Task<CanvasActionResult>> invoke)
    {
        Descriptor = descriptor;
        Invoke = invoke;
    }

    /// <summary>Name, description and argument schema.</summary>
    public CanvasActionDescriptor Descriptor { get; }

    internal Func<JsonElement?, CanvasActionContext, CancellationToken, Task<CanvasActionResult>> Invoke { get; }
}
