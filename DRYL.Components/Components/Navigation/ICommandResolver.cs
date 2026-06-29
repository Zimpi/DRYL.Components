namespace DRYL.Components;

/// <summary>The narrow AI seam. An implementation turns a natural-language query plus the registered
/// commands into at most one resolved command + filled arguments. The core ships none and never
/// references any AI package; supply one (e.g. <c>DRYL.Components.Agents.DrylAiCommandResolver</c>)
/// via <see cref="DrylCommandPalette.Resolver"/>.</summary>
public interface ICommandResolver
{
    /// <summary>Resolves a query against the available commands. Returns <c>null</c> when nothing fits.
    /// The implementation must not execute the command — execution is the palette's, after confirmation.</summary>
    Task<CommandResolution?> ResolveAsync(
        string query, IReadOnlyList<DrylCommand> commands, CancellationToken ct);
}
