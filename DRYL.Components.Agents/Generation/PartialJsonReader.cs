using System.Text;
using System.Text.Json;

namespace DRYL.Components.Agents.Generation;

/// <summary>
/// Accumulates a streamed JSON buffer and produces a progressively-richer partial
/// snapshot of <typeparamref name="T"/> on every chunk. Open strings surface their
/// content-so-far, so titles and text grow character by character. On a parse failure
/// it holds the last good snapshot rather than flickering back to <c>null</c>.
/// </summary>
public sealed class PartialJsonReader<T>
{
    private static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly JsonSerializerOptions _options;
    private readonly StringBuilder _buffer = new();
    private T? _last;

    /// <summary>Creates the reader, optionally with custom serializer options.</summary>
    public PartialJsonReader(JsonSerializerOptions? options = null) => _options = options ?? Default;

    /// <summary>The raw accumulated JSON buffer.</summary>
    public string Buffer => _buffer.ToString();

    /// <summary>The most recent successfully-parsed snapshot (may be partial), or <c>null</c>.</summary>
    public T? Current => _last;

    /// <summary>Append a chunk and return the current best snapshot.</summary>
    public T? Append(string chunk)
    {
        _buffer.Append(chunk);
        try
        {
            var repaired = JsonPartialRepair.Close(_buffer.ToString());
            var snapshot = JsonSerializer.Deserialize<T>(repaired, _options);
            if (snapshot is not null) _last = snapshot;
        }
        catch (JsonException)
        {
            // hold last good
        }
        return _last;
    }
}
