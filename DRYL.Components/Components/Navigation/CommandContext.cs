using System.Globalization;

namespace DRYL.Components;

/// <summary>The single payload passed to <see cref="DrylCommand.OnRun"/> — identical whether the
/// command was run by click, keyboard, or an AI resolution. Exposes the resolved arguments and a
/// cancellation token.</summary>
public sealed class CommandContext
{
    /// <summary>Creates a context from a resolved argument set.</summary>
    public CommandContext(
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        Arguments = arguments;
        CancellationToken = cancellationToken;
    }

    /// <summary>The resolved arguments by name (empty when the command takes none).</summary>
    public IReadOnlyDictionary<string, object?> Arguments { get; }

    /// <summary>Cancelled if the palette closes or the circuit tears down mid-run.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Reads an argument as <typeparamref name="T"/>; converts culture-invariantly and
    /// returns <c>default</c> when missing or unconvertible.</summary>
    public T? GetArgument<T>(string name)
    {
        if (!Arguments.TryGetValue(name, out var value) || value is null)
            return default;
        if (value is T typed)
            return typed;
        try
        {
            var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
        }
        catch
        {
            return default;
        }
    }
}
