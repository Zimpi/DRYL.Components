using System.Collections.Concurrent;

namespace DRYL.Components.Canvas;

/// <summary>What a document listing shows without loading the artifact itself.</summary>
/// <param name="Id">Store key.</param>
/// <param name="Title">Document title.</param>
/// <param name="SavedAt">When it was last written (UTC).</param>
/// <param name="ViewCount">How many views it holds.</param>
public sealed record CanvasDocumentInfo(string Id, string Title, DateTimeOffset SavedAt, int ViewCount);

/// <summary>
/// Where canvas documents live. DRYL ships the contract and an in-memory implementation and no
/// database code at all — the host keeps owning its data.
/// </summary>
/// <remarks>
/// Task-based on purpose: on WebAssembly a host implements this over HTTP or <c>localStorage</c>
/// without a single server-side construct in the contract.
/// </remarks>
public interface ICanvasDocumentStore
{
    /// <summary>
    /// Writes the document. A document without an <see cref="CanvasDocument.Id"/> gets a new one,
    /// which is written back onto the instance and returned.
    /// </summary>
    /// <param name="document">The document to store.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<string> SaveAsync(CanvasDocument document, CancellationToken ct = default);

    /// <summary>Reads a document, or null when the id is unknown.</summary>
    /// <param name="id">The store key.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<CanvasDocument?> LoadAsync(string id, CancellationToken ct = default);

    /// <summary>Lists the stored documents, newest first.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<CanvasDocumentInfo>> ListAsync(CancellationToken ct = default);

    /// <summary>Deletes a document. An unknown id is not an error.</summary>
    /// <param name="id">The store key.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(string id, CancellationToken ct = default);
}

/// <summary>
/// The in-process reference implementation — enough for demos, tests and single-node prototypes,
/// and the shape a real store should behave like.
/// </summary>
/// <remarks>
/// Stores the serialized form, not the object: a caller cannot mutate a loaded document back into
/// the store, and every save exercises the serialization path.
/// </remarks>
public sealed class InMemoryCanvasDocumentStore : ICanvasDocumentStore
{
    private readonly ConcurrentDictionary<string, string> _documents = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task<string> SaveAsync(CanvasDocument document, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.Id ??= Guid.NewGuid().ToString("n");
        document.SavedAt = DateTimeOffset.UtcNow;
        _documents[document.Id] = document.ToJson();
        return Task.FromResult(document.Id);
    }

    /// <inheritdoc />
    public Task<CanvasDocument?> LoadAsync(string id, CancellationToken ct = default)
    {
        if (id is null || !_documents.TryGetValue(id, out var json))
            return Task.FromResult<CanvasDocument?>(null);

        return Task.FromResult(CanvasDocument.TryFromJson(json, out var doc, out _) ? doc : null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CanvasDocumentInfo>> ListAsync(CancellationToken ct = default)
    {
        var list = _documents.Values
            .Select(json => CanvasDocument.TryFromJson(json, out var d, out _) ? d : null)
            .Where(d => d is not null)
            .Select(d => new CanvasDocumentInfo(d!.Id!, d.Title ?? "Canvas", d.SavedAt, d.Views?.Count ?? 0))
            .OrderByDescending(i => i.SavedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<CanvasDocumentInfo>>(list);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        if (id is not null) _documents.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
