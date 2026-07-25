using DRYL.Components.Canvas;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>The persistence contract DRYL ships — the host owns the database.</summary>
public class CanvasDocumentStoreTests
{
    private static CanvasDocument Doc(string title)
    {
        var ws = new CanvasWorkspace();
        ws.Open(title).Spec = new CanvasSpec { Title = title, Root = new CanvasNode { Id = "r", Type = "stack" } };
        return CanvasDocument.Capture(ws, title);
    }

    [Fact]
    public async Task Save_mints_an_id_and_writes_it_back()
    {
        var store = new InMemoryCanvasDocumentStore();
        var doc = Doc("Overview");

        var id = await store.SaveAsync(doc);

        Assert.False(string.IsNullOrWhiteSpace(id));
        Assert.Equal(id, doc.Id);
    }

    [Fact]
    public async Task Save_with_a_known_id_overwrites()
    {
        var store = new InMemoryCanvasDocumentStore();
        var doc = Doc("Overview");
        var id = await store.SaveAsync(doc);

        doc.Title = "Renamed";
        var again = await store.SaveAsync(doc);

        Assert.Equal(id, again);
        Assert.Single(await store.ListAsync());
        Assert.Equal("Renamed", (await store.LoadAsync(id))!.Title);
    }

    [Fact]
    public async Task Load_returns_a_document_that_the_caller_cannot_mutate_in_the_store()
    {
        var store = new InMemoryCanvasDocumentStore();
        var id = await store.SaveAsync(Doc("Overview"));

        var loaded = await store.LoadAsync(id);
        loaded!.Title = "tampered";

        Assert.Equal("Overview", (await store.LoadAsync(id))!.Title);
    }

    [Fact]
    public async Task List_returns_the_newest_first()
    {
        var store = new InMemoryCanvasDocumentStore();
        var older = Doc("Older");
        await store.SaveAsync(older);
        await store.SaveAsync(Doc("Newer"));

        var list = await store.ListAsync();

        Assert.Equal(2, list.Count);
        Assert.Equal("Newer", list[0].Title);
        Assert.Equal(1, list[0].ViewCount);
    }

    [Fact]
    public async Task Load_of_an_unknown_id_is_null_and_Delete_is_silent()
    {
        var store = new InMemoryCanvasDocumentStore();

        Assert.Null(await store.LoadAsync("nope"));
        await store.DeleteAsync("nope");
    }

    [Fact]
    public async Task Delete_removes_the_document()
    {
        var store = new InMemoryCanvasDocumentStore();
        var id = await store.SaveAsync(Doc("Overview"));

        await store.DeleteAsync(id);

        Assert.Null(await store.LoadAsync(id));
        Assert.Empty(await store.ListAsync());
    }

    [Fact]
    public void AddDrylCanvasDocumentStore_registers_the_in_memory_store_as_a_singleton()
    {
        var provider = new ServiceCollection().AddDrylCanvasDocumentStore().BuildServiceProvider();

        var store = provider.GetService<ICanvasDocumentStore>();

        Assert.IsType<InMemoryCanvasDocumentStore>(store);
        Assert.Same(store, provider.GetService<ICanvasDocumentStore>());
    }

    [Fact]
    public void A_host_store_registered_first_wins()
    {
        var provider = new ServiceCollection()
            .AddDrylCanvasDocumentStore<HostStore>()
            .AddDrylCanvasDocumentStore()
            .BuildServiceProvider();

        Assert.IsType<HostStore>(provider.GetService<ICanvasDocumentStore>());
    }

    private sealed class HostStore : ICanvasDocumentStore
    {
        public Task<string> SaveAsync(CanvasDocument document, CancellationToken ct = default) =>
            Task.FromResult("x");

        public Task<CanvasDocument?> LoadAsync(string id, CancellationToken ct = default) =>
            Task.FromResult<CanvasDocument?>(null);

        public Task<IReadOnlyList<CanvasDocumentInfo>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CanvasDocumentInfo>>(Array.Empty<CanvasDocumentInfo>());

        public Task DeleteAsync(string id, CancellationToken ct = default) => Task.CompletedTask;
    }
}
