using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests;

public class DrylAiCommandPaletteTests : BunitContext
{
    public DrylAiCommandPaletteTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private sealed class NullResolver : ICommandResolver
    {
        public Task<CommandResolution?> ResolveAsync(
            string q, IReadOnlyList<DrylCommand> c, CancellationToken ct)
            => Task.FromResult<CommandResolution?>(null);
    }

    [Fact]
    public void Forwards_children_to_inner_palette()
    {
        Services.AddScoped<ICommandResolver>(_ => new NullResolver());
        var cut = Render<DrylAiCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .AddChildContent<DrylCommand>(c => c.Add(x => x.Title, "AI command")));

        cut.WaitForAssertion(() =>
            Assert.Contains(cut.FindAll("[role=option]"), o => o.TextContent.Contains("AI command")),
            TimeSpan.FromSeconds(2));
    }
}
