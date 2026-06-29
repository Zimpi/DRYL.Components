namespace DRYL.Components;

/// <summary>Default in-memory <see cref="ICommandRegistry"/>. Registered scoped by
/// <c>AddDrylComponents()</c> (one per Blazor circuit).</summary>
public sealed class CommandRegistry : ICommandRegistry
{
    private readonly List<DrylCommand> _commands = new();

    /// <inheritdoc />
    public IReadOnlyList<DrylCommand> Commands => _commands;

    /// <inheritdoc />
    public void Add(DrylCommand command)
    {
        if (_commands.Any(c => c.ResolvedId == command.ResolvedId)) return;
        _commands.Add(command);
        OnChanged?.Invoke();
    }

    /// <inheritdoc />
    public void Remove(string id)
    {
        if (_commands.RemoveAll(c => c.ResolvedId == id) > 0)
            OnChanged?.Invoke();
    }

    /// <inheritdoc />
    public event Action? OnChanged;
}
