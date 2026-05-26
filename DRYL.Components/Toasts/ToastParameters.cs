using System.Collections;
using System.Collections.Generic;

namespace DRYL.Components.Toasts;

/// <summary>
/// A bag of parameter values forwarded to a custom toast body component. Keys match the
/// component's <c>[Parameter]</c> property names.
/// </summary>
/// <example>
/// <code>
/// var p = new ToastParameters { ["ProjectId"] = id, ["OnUndo"] = EventCallback.Factory.Create(this, Undo) };
/// Toasts.Show&lt;UndoToastBody&gt;(p, new ToastOptions { Title = "Project archived" });
/// </code>
/// </example>
public sealed class ToastParameters : IEnumerable<KeyValuePair<string, object>>
{
    private readonly Dictionary<string, object> _values = new();

    /// <summary>Number of parameters set.</summary>
    public int Count => _values.Count;

    /// <summary>Get or set a parameter by name.</summary>
    public object? this[string name]
    {
        get => _values.TryGetValue(name, out var v) ? v : null;
        set
        {
            if (value is null) _values.Remove(name);
            else _values[name] = value;
        }
    }

    /// <summary>Add or replace a parameter. Null values are skipped.</summary>
    public ToastParameters Add(string name, object? value)
    {
        if (value is null) _values.Remove(name);
        else _values[name] = value;
        return this;
    }

    /// <summary>True if a parameter with this name has been set.</summary>
    public bool Contains(string name) => _values.ContainsKey(name);

    /// <summary>Materialize as a dictionary suitable for <c>DynamicComponent.Parameters</c>.</summary>
    public IDictionary<string, object> ToDictionary() => new Dictionary<string, object>(_values);

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();
}
