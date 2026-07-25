namespace DRYL.Components.Canvas;

/// <summary>
/// The application-wide set of named canvas actions, filled at startup by
/// <c>AddDrylCanvasAction</c>. Registered as a singleton; the per-scope
/// <see cref="ICanvasActionService"/> runs its entries against the current scope.
/// </summary>
public sealed class CanvasActionRegistry
{
    private readonly Dictionary<string, CanvasActionSource> _actions = new(StringComparer.Ordinal);
    private readonly List<CanvasActionDescriptor> _descriptors = new();

    /// <summary>Every registered action, in registration order.</summary>
    public IReadOnlyList<CanvasActionDescriptor> Descriptors => _descriptors;

    /// <summary>Looks up an action by its registered name.</summary>
    public bool TryGet(string name, out CanvasActionSource action) => _actions.TryGetValue(name, out action!);

    internal void Add(CanvasActionSource action)
    {
        if (!_actions.TryAdd(action.Descriptor.Name, action))
            throw new InvalidOperationException(
                $"A canvas action named '{action.Descriptor.Name}' is already registered.");
        _descriptors.Add(action.Descriptor);
    }
}
