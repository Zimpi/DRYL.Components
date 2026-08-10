namespace DRYL.Components.Canvas;

/// <summary>
/// What a <c>form</c> node shares with its interactive children: which required fields failed
/// the last submit. Cascaded (fixed) from the form's view; children re-render via
/// <see cref="OnChanged"/>, and typing into a field clears its flag immediately.
/// </summary>
internal sealed class CanvasFormScope
{
    private readonly HashSet<string> _missing = new(StringComparer.Ordinal);

    /// <summary>Raised when the missing set changes; subscribed views re-render.</summary>
    public event Action? OnChanged;

    /// <summary>True when <paramref name="name"/> failed the last submit and has not been edited since.</summary>
    public bool IsMissing(string name) => _missing.Contains(name);

    internal void SetMissing(IEnumerable<string> names)
    {
        _missing.Clear();
        foreach (var n in names) _missing.Add(n);
        OnChanged?.Invoke();
    }

    internal void Clear(string name)
    {
        if (_missing.Remove(name)) OnChanged?.Invoke();
    }
}
