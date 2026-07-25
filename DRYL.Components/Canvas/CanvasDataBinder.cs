using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace DRYL.Components.Canvas;

/// <summary>What one bound node currently has to show.</summary>
public sealed class CanvasBindingState
{
    internal CanvasBindingState(bool loading, CanvasData? data, string? error)
    {
        Loading = loading;
        Data = data;
        Error = error;
    }

    /// <summary>A load is in flight.</summary>
    public bool Loading { get; }

    /// <summary>The last good result, or <c>null</c> if there has never been one.</summary>
    public CanvasData? Data { get; }

    /// <summary>A short, user-facing failure sentence naming the source — never a stack trace.</summary>
    public string? Error { get; }

    /// <summary>True once a good value has arrived. A later failure keeps it: "briefly
    /// disturbed" is not "broken", so the value stays on screen with a small marker.</summary>
    public bool HasValue => Data is not null;
}

/// <summary>
/// Resolves one canvas's <c>data</c> bindings. Owned by a <c>DrylCanvas</c> instance, never by the
/// application — two canvases on a page share nothing.
///
/// <para>Bindings are keyed by <c>source</c> plus their <em>resolved</em> parameters, so three stat
/// nodes on <c>orders.open</c> are one key and one handler call. Four things trigger a load: the
/// first render of a binding, a change to a form field a parameter references (debounced), the
/// interval timer, and a host <c>Invalidate</c>. Per key at most one load runs; a newer one cancels
/// the older, and a late result carrying a stale sequence number is dropped.</para>
///
/// <para>A refresh that produces the same value is silent — no pulse, no render. A refresh with a
/// new value stamps the pulse tracker, which is exactly what an AI <c>setProps</c> does: one
/// language for "something changed here", whoever changed it (A8).</para>
/// </summary>
public sealed class CanvasDataBinder : IAsyncDisposable
{
    // An input pause, not an animation — deliberately not a --dur-* token. Without it every
    // keystroke in an inputText that a parameter references would hit the database.
    private const int DebounceMs = 300;
    private const int MinIntervalSeconds = 5;

    private readonly ICanvasDataService _data;
    private readonly CanvasFormState _form;
    private readonly CanvasPulseTracker _pulse;
    private readonly ILogger? _log;

    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);   // key -> entry
    private readonly Dictionary<string, string> _nodeKeys = new(StringComparer.Ordinal); // nodeId -> key
    private readonly Dictionary<string, CanvasDataBinding> _nodeBindings = new(StringComparer.Ordinal);

    private Timer? _timer;
    private CancellationTokenSource? _debounce;
    private readonly CancellationTokenSource _disposing = new();
    private int _seq;
    private bool _disposed;

    /// <summary>Creates a binder for one canvas.</summary>
    public CanvasDataBinder(ICanvasDataService data, CanvasFormState form, CanvasPulseTracker pulse,
                            ILogger? log = null)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _form = form ?? throw new ArgumentNullException(nameof(form));
        _pulse = pulse ?? throw new ArgumentNullException(nameof(pulse));
        _log = log;

        _form.OnChanged += HandleFormChanged;
        _data.Invalidated += HandleInvalidated;
    }

    /// <summary>Raised whenever something a node renders has changed. The canvas re-renders.</summary>
    public event Action? OnChanged;

    /// <summary>True once at least one node in this artifact carries a <c>data</c> binding —
    /// the header's refresh button appears only then.</summary>
    public bool HasBindings => _nodeKeys.Count > 0;

    /// <summary>True while any binding is loading.</summary>
    public bool Busy => _entries.Values.Any(e => e.Loading);

    /// <summary>What <paramref name="nodeId"/> should render, or <c>null</c> if it is not bound.</summary>
    public CanvasBindingState? StateOf(string nodeId) =>
        _nodeKeys.TryGetValue(nodeId, out var key) && _entries.TryGetValue(key, out var entry)
            ? new CanvasBindingState(entry.Loading, entry.Data, entry.Error)
            : null;

    /// <summary>
    /// Announces a node's binding. Called by <c>CanvasNodeView</c> on every parameter set; the first
    /// call for a key starts the initial load, and a later call whose resolved parameters moved
    /// (a <c>$field</c> changed) re-keys the node and loads the new key.
    /// </summary>
    public void Register(string nodeId, CanvasDataBinding binding)
    {
        if (_disposed || string.IsNullOrWhiteSpace(binding.Source)) return;

        _nodeBindings[nodeId] = binding;
        var (key, resolved, fields) = Resolve(binding);

        if (_nodeKeys.TryGetValue(nodeId, out var oldKey))
        {
            if (oldKey == key) return;
            Detach(nodeId, oldKey);
        }

        _nodeKeys[nodeId] = key;
        if (!_entries.TryGetValue(key, out var entry))
        {
            _entries[key] = entry = new Entry(binding.Source!, resolved, fields, IntervalOf(binding.Refresh));
            _ = LoadAsync(entry, initial: true);
        }
        entry.Nodes.Add(nodeId);
        SyncTimer();
    }

    /// <summary>Forgets a node's binding — its key is dropped once no node references it.</summary>
    public void Unregister(string nodeId)
    {
        _nodeBindings.Remove(nodeId);
        if (_nodeKeys.TryGetValue(nodeId, out var key)) Detach(nodeId, key);
        SyncTimer();
    }

    /// <summary>Drops every binding — a different artifact took the canvas over.</summary>
    public void Reset()
    {
        foreach (var entry in _entries.Values) entry.Cancel();
        _entries.Clear();
        _nodeKeys.Clear();
        _nodeBindings.Clear();
        SyncTimer();
    }

    /// <summary>Reloads every binding — the header's refresh button.</summary>
    public Task RefreshAllAsync()
    {
        var loads = _entries.Values.Select(e => LoadAsync(e, initial: false)).ToArray();
        Raise();
        return Task.WhenAll(loads);
    }

    // ---- triggers ----

    // A field change may move any number of keys, and a user typing moves them on every keystroke.
    // One debounce window for the whole canvas keeps that at one round of loads.
    private void HandleFormChanged()
    {
        if (_disposed) return;
        _debounce?.Cancel();
        _debounce?.Dispose();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(_disposing.Token);
        _debounce = cts;
        _ = DebouncedReresolveAsync(cts.Token);
    }

    private async Task DebouncedReresolveAsync(CancellationToken ct)
    {
        try { await Task.Delay(DebounceMs, ct).ConfigureAwait(false); }
        catch (OperationCanceledException) { return; }
        if (_disposed) return;
        ReresolveFieldBindings();
    }

    private void ReresolveFieldBindings()
    {
        // Only bindings that actually reference a field can have moved; everything else keeps
        // its key, its entry and its value untouched.
        var affected = _nodeBindings.Where(kv => HasFieldReference(kv.Value)).Select(kv => kv.Key).ToList();
        var moved = false;
        foreach (var nodeId in affected)
        {
            var before = _nodeKeys.TryGetValue(nodeId, out var k) ? k : null;
            Register(nodeId, _nodeBindings[nodeId]);
            if (before != (_nodeKeys.TryGetValue(nodeId, out var after) ? after : null)) moved = true;
        }
        if (moved) Raise();
    }

    private void HandleInvalidated(CanvasInvalidation notice)
    {
        if (_disposed) return;
        var hit = false;
        foreach (var entry in _entries.Values)
        {
            if (entry.Source != notice.Source) continue;
            if (notice.ParamsKey is not null && notice.ParamsKey != CanvasDataKey.Canonicalize(entry.Params)) continue;
            hit = true;
            _ = LoadAsync(entry, initial: false);
        }
        if (hit) Raise();
    }

    // One timer for the whole canvas — not one per node — and only while an interval binding
    // exists. It ticks once a second and collects whatever has come due.
    private void SyncTimer()
    {
        var wanted = _entries.Values.Any(e => e.IntervalSeconds is > 0);
        if (wanted && _timer is null)
            _timer = new Timer(_ => Tick(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        else if (!wanted && _timer is not null)
        {
            _timer.Dispose();
            _timer = null;
        }
    }

    private void Tick()
    {
        if (_disposed) return;
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in _entries.Values.ToList())
        {
            if (entry.IntervalSeconds is not > 0 || entry.Loading) continue;
            if (now - entry.LoadedAt < TimeSpan.FromSeconds(entry.IntervalSeconds.Value)) continue;
            _ = LoadAsync(entry, initial: false);
        }
    }

    // ---- loading ----

    private async Task LoadAsync(Entry entry, bool initial)
    {
        if (_disposed) return;

        // At most one load per key: a newer one cancels the older, and the sequence number
        // makes a late straggler's result identifiable and droppable.
        entry.Cancel();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(_disposing.Token);
        entry.Cts = cts;
        var seq = ++_seq;
        entry.Seq = seq;
        entry.Loading = true;
        if (!initial) Raise();

        CanvasData? result = null;
        string? error = null;
        try
        {
            result = await _data.LoadAsync(entry.Source, entry.Params, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            // A handler that throws must never reach the renderer, let alone the circuit.
            _log?.LogError(ex, "Canvas data source '{Source}' failed.", entry.Source);
            error = $"Source '{entry.Source}' is unavailable.";
        }

        if (_disposed || entry.Seq != seq) return;   // superseded — drop the straggler
        entry.Loading = false;
        entry.LoadedAt = DateTimeOffset.UtcNow;

        if (error is not null)
        {
            entry.Error = error;
            Raise();
            return;
        }

        entry.Error = null;
        var fingerprint = Fingerprint(result!);
        if (entry.Fingerprint == fingerprint)
        {
            // Same numbers as before. Rendering it again would blink the dashboard every
            // 30 seconds for nothing, so this is where a refresh stays silent.
            Raise();
            return;
        }

        var hadValue = entry.Data is not null;
        entry.Fingerprint = fingerprint;
        entry.Data = result;

        // The first arrival replaces a skeleton, which is motion enough; only a *change*
        // of an already-visible value earns the pulse.
        if (hadValue)
            foreach (var nodeId in entry.Nodes) _pulse.Stamp(nodeId);

        Raise();
    }

    private void Raise() => OnChanged?.Invoke();

    // ---- parameter resolution ----

    private (string Key, JsonElement? Params, HashSet<string> Fields) Resolve(CanvasDataBinding binding)
    {
        var fields = new HashSet<string>(StringComparer.Ordinal);
        JsonElement? resolved = null;

        if (binding.Params is { ValueKind: JsonValueKind.Object } raw)
        {
            var obj = new JsonObject();
            foreach (var p in raw.EnumerateObject())
            {
                if (FieldReference(p.Value) is { } field)
                {
                    fields.Add(field);
                    obj[p.Name] = ToNode(_form.Get(field));
                }
                else
                {
                    obj[p.Name] = JsonNode.Parse(p.Value.GetRawText());
                }
            }
            resolved = JsonSerializer.SerializeToElement(obj);
        }

        return (CanvasDataKey.Of(binding.Source!, resolved), resolved, fields);
    }

    /// <summary>The field name of a <c>{ "$field": "…" }</c> reference, or <c>null</c> for a literal.</summary>
    internal static string? FieldReference(JsonElement value) =>
        value.ValueKind == JsonValueKind.Object &&
        value.TryGetProperty("$field", out var f) &&
        f.ValueKind == JsonValueKind.String
            ? f.GetString()
            : null;

    internal static bool HasFieldReference(CanvasDataBinding binding) =>
        binding.Params is { ValueKind: JsonValueKind.Object } p &&
        p.EnumerateObject().Any(x => FieldReference(x.Value) is not null);

    private static JsonNode? ToNode(object? value) => value switch
    {
        null => null,
        string s => JsonValue.Create(s),
        bool b => JsonValue.Create(b),
        double d => JsonValue.Create(d),
        int i => JsonValue.Create(i),
        long l => JsonValue.Create(l),
        decimal m => JsonValue.Create(m),
        _ => JsonValue.Create(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)),
    };

    /// <summary>The interval in seconds, or <c>null</c> for manual refresh. Values below the
    /// five-second floor are lifted to it (the validator says so in the receipt).</summary>
    internal static int? IntervalOf(string? refresh)
    {
        if (string.IsNullOrWhiteSpace(refresh) || refresh.Equals("manual", StringComparison.OrdinalIgnoreCase))
            return null;
        if (!TryParseInterval(refresh, out var seconds)) return null;
        return Math.Max(seconds, MinIntervalSeconds);
    }

    /// <summary>Parses <c>interval:&lt;n&gt;s</c>; false when the syntax is wrong.</summary>
    internal static bool TryParseInterval(string refresh, out int seconds)
    {
        seconds = 0;
        const string prefix = "interval:";
        if (!refresh.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        var body = refresh[prefix.Length..].Trim();
        if (!body.EndsWith("s", StringComparison.OrdinalIgnoreCase)) return false;
        return int.TryParse(body[..^1], System.Globalization.NumberStyles.Integer,
                            System.Globalization.CultureInfo.InvariantCulture, out seconds) && seconds > 0;
    }

    // A refresh must only redraw when a value actually moved — comparing the canonical rendering
    // of the result is both the cheapest and the most honest way to decide that.
    private static string Fingerprint(CanvasData data) => data switch
    {
        CanvasScalarData s => JsonSerializer.Serialize(new { s.Value, s.Text, s.Delta, s.Direction }, CanvasJson.Options),
        CanvasSeriesData s => JsonSerializer.Serialize(new { s.Labels, s.Series }, CanvasJson.Options),
        CanvasSegmentData s => JsonSerializer.Serialize(new { s.Segments }, CanvasJson.Options),
        CanvasRowData r => JsonSerializer.Serialize(new { r.Columns, r.Rows }, CanvasJson.Options),
        _ => string.Empty,
    };

    private void Detach(string nodeId, string key)
    {
        _nodeKeys.Remove(nodeId);
        if (!_entries.TryGetValue(key, out var entry)) return;
        entry.Nodes.Remove(nodeId);
        if (entry.Nodes.Count > 0) return;
        entry.Cancel();
        _entries.Remove(key);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _form.OnChanged -= HandleFormChanged;
        _data.Invalidated -= HandleInvalidated;

        if (_timer is not null) await _timer.DisposeAsync().ConfigureAwait(false);
        _timer = null;

        _disposing.Cancel();
        foreach (var entry in _entries.Values) entry.Cancel();
        _entries.Clear();

        _debounce?.Dispose();
        _disposing.Dispose();
    }

    /// <summary>One dedupe key's state — the nodes on it, the last good value and the running load.</summary>
    private sealed class Entry
    {
        public Entry(string source, JsonElement? parameters, HashSet<string> fields, int? intervalSeconds)
        {
            Source = source;
            Params = parameters;
            Fields = fields;
            IntervalSeconds = intervalSeconds;
        }

        public string Source { get; }
        public JsonElement? Params { get; }
        public HashSet<string> Fields { get; }
        public int? IntervalSeconds { get; }

        /// <summary>Every node rendering this key — that is the dedupe.</summary>
        public HashSet<string> Nodes { get; } = new(StringComparer.Ordinal);

        public bool Loading { get; set; }
        public CanvasData? Data { get; set; }
        public string? Error { get; set; }
        public string? Fingerprint { get; set; }
        public int Seq { get; set; }
        public DateTimeOffset LoadedAt { get; set; }
        public CancellationTokenSource? Cts { get; set; }

        public void Cancel()
        {
            var cts = Cts;
            Cts = null;
            if (cts is null) return;
            try { cts.Cancel(); } catch (ObjectDisposedException) { /* already gone */ }
            cts.Dispose();
        }
    }
}
