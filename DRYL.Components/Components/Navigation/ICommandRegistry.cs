namespace DRYL.Components;

/// <summary>The shared, scoped registry of <see cref="DrylCommand"/>s a palette renders. Both the
/// declarative <see cref="DrylCommand"/> children and consumer code (for dynamic, context-dependent
/// commands) feed the same instance; the palette renders the union, de-duplicated by
/// <see cref="DrylCommand.ResolvedId"/>.</summary>
public interface ICommandRegistry
{
    /// <summary>The currently registered commands, in registration order.</summary>
    IReadOnlyList<DrylCommand> Commands { get; }

    /// <summary>Adds a command. A command whose id is already present is ignored.</summary>
    void Add(DrylCommand command);

    /// <summary>Removes the command with the given id, if present.</summary>
    void Remove(string id);

    /// <summary>Raised whenever the command set changes.</summary>
    event Action? OnChanged;
}
