namespace DRYL.Components.Agents;

/// <summary>Live values of a canvas's interactive nodes, keyed by their "name" prop.</summary>
public sealed class CanvasFormState
{
    private readonly Dictionary<string, object?> _values = new();

    /// <summary>Gets the value for the given name, or null if not found.</summary>
    public object? Get(string name)
    {
        _values.TryGetValue(name, out var value);
        return value;
    }

    /// <summary>Gets the typed value for the given name, or default(T) if not found or on type mismatch.</summary>
    public T? Get<T>(string name)
    {
        if (_values.TryGetValue(name, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>Sets the value for the given name and fires <see cref="OnChanged"/>.</summary>
    public void Set(string name, object? value)
    {
        _values[name] = value;
        OnChanged?.Invoke();
    }

    /// <summary>Returns a defensive copy of all current values.</summary>
    public IReadOnlyDictionary<string, object?> Snapshot() => new Dictionary<string, object?>(_values);

    /// <summary>Removes all values and fires <see cref="OnChanged"/>. Internal — the canvas
    /// calls this when a fresh artifact begins (see DrylCanvasRun.ArtifactEpoch).</summary>
    internal void Clear()
    {
        _values.Clear();
        OnChanged?.Invoke();
    }

    /// <summary>Fired whenever a value is set via <see cref="Set"/>.</summary>
    public event Action? OnChanged;
}
