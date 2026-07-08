using System.Runtime.CompilerServices;
using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;
using DRYL.Components.Agents.Generation;
using Microsoft.AspNetCore.Components;

namespace DRYL.Components.Tests.Agents;

public class DrylAiGenerateTests : BunitContext
{
    public sealed class Recipe { public string? Title { get; set; } }

    private static async IAsyncEnumerable<string> Stream(
        string[] chunks, [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var c in chunks) { await Task.Yield(); yield return c; }
    }

    [Fact]
    public void Renders_partial_then_complete_snapshot()
    {
        var src = Stream(new[] { "{\"title\":\"Pan", "cakes\"}" });

        var cut = Render<DrylAiGenerate<Recipe>>(p => p
            .Add(x => x.Source, src)
            .Add(x => x.ChildContent, (RenderFragment<GenerationSnapshot<Recipe>>)(snap =>
                builder => builder.AddContent(0, snap.Value?.Title))));

        cut.WaitForAssertion(() => Assert.Contains("Pancakes", cut.Markup));
    }

    [Fact]
    public void Exposes_raw_buffer_on_snapshot()
    {
        var src = Stream(new[] { "{\"title\":\"Pan", "cakes\"}" });

        GenerationSnapshot<Recipe>? seen = null;
        var cut = Render<DrylAiGenerate<Recipe>>(p => p
            .Add(x => x.Source, src)
            .Add(x => x.ChildContent, (RenderFragment<GenerationSnapshot<Recipe>>)(snap =>
                builder => { seen = snap; })));

        // The snapshot instance is reused; at completion Raw holds the full accumulated buffer.
        cut.WaitForAssertion(() => Assert.Equal("{\"title\":\"Pancakes\"}", seen!.Raw));
    }
}
