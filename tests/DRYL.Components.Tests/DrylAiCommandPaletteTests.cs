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

    [Fact]
    public void Leaves_the_default_words_to_the_inner_palette()
    {
        Services.AddScoped<ICommandResolver>(_ => new NullResolver());
        var cut = Render<DrylAiCommandPalette>(ps => ps.Add(p => p.Open, true));

        cut.WaitForAssertion(() =>
            Assert.Equal(CommandPaletteTexts.Default.Placeholder,
                         cut.Find("input").GetAttribute("placeholder")),
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void A_set_placeholder_wins_over_the_default()
    {
        Services.AddScoped<ICommandResolver>(_ => new NullResolver());
        var cut = Render<DrylAiCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Placeholder, "Befehl suchen…"));

        cut.WaitForAssertion(() =>
            Assert.Equal("Befehl suchen…", cut.Find("input").GetAttribute("placeholder")),
            TimeSpan.FromSeconds(2));
    }
}
