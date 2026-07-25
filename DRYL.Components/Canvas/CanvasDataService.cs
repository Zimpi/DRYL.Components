using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DRYL.Components.Canvas;

/// <summary>A host's notice that a source's result is stale. <see cref="ParamsKey"/> is
/// <c>null</c> for "every binding on this source".</summary>
public sealed record CanvasInvalidation(string Source, string? ParamsKey);

/// <summary>
/// The per-scope view onto the registered canvas data sources: what exists, how to run it,
/// and how the host tells a canvas that something changed underneath it.
/// </summary>
public interface ICanvasDataService
{
    /// <summary>Every registered source. Also the material for the model's prompt block.</summary>
    IReadOnlyList<CanvasDataDescriptor> Descriptors { get; }

    /// <summary>Marks every binding on <paramref name="source"/> stale; bound canvases reload.</summary>
    void Invalidate(string source);

    /// <summary>Marks only the bindings on <paramref name="source"/> whose parameters match
    /// <paramref name="parameters"/> stale. Pass an anonymous object or the parameter record.</summary>
    void Invalidate(string source, object parameters);

    /// <summary>Infrastructure — raised by <see cref="Invalidate(string)"/>. Bound canvases listen; hosts do not.</summary>
    event Action<CanvasInvalidation>? Invalidated;

    /// <summary>Infrastructure — runs a registered source against this scope. Bound canvases call it; hosts do not.</summary>
    Task<CanvasData> LoadAsync(string source, JsonElement? parameters, CancellationToken ct);
}

/// <inheritdoc cref="ICanvasDataService" />
internal sealed class CanvasDataService : ICanvasDataService
{
    private readonly CanvasDataRegistry _registry;
    private readonly IServiceProvider _scope;

    // The block goes into *every* generation, so a large registry is a standing token cost.
    // Once per process is the right cadence for saying so — the real answer is the catalog
    // compression of phase 4, not a hard limit here.
    private static int _crowdedWarned;

    public CanvasDataService(CanvasDataRegistry registry, IServiceProvider scope)
    {
        _registry = registry;
        _scope = scope;

        if (registry.Descriptors.Count >= CanvasDataPrompt.CrowdedAt &&
            Interlocked.Exchange(ref _crowdedWarned, 1) == 0)
        {
            (scope.GetService(typeof(ILogger<CanvasDataService>)) as ILogger)?.LogWarning(
                "{Count} canvas data sources are registered. Their descriptors go into every artifact " +
                "generation — keep each description to one line, or the prompt grows with the catalog.",
                registry.Descriptors.Count);
        }
    }

    public IReadOnlyList<CanvasDataDescriptor> Descriptors => _registry.Descriptors;

    public event Action<CanvasInvalidation>? Invalidated;

    public void Invalidate(string source) => Invalidated?.Invoke(new CanvasInvalidation(source, null));

    public void Invalidate(string source, object parameters) =>
        Invalidated?.Invoke(new CanvasInvalidation(
            source, CanvasDataKey.Canonicalize(JsonSerializer.SerializeToElement(parameters, CanvasJson.Options))));

    public Task<CanvasData> LoadAsync(string source, JsonElement? parameters, CancellationToken ct)
    {
        if (!_registry.TryGet(source, out var entry))
            throw new InvalidOperationException($"No canvas data source named '{source}' is registered.");
        return entry.Invoke(parameters, _scope, ct);
    }
}
