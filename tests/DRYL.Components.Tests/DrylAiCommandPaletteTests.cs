using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;
using System.Reflection;
using Microsoft.AspNetCore.Components;
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

    private static Dictionary<string, Type> ParametersOf(Type component) =>
        component.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
            .ToDictionary(p => p.Name, p => p.PropertyType);

    [Fact]
    public void Carries_every_parameter_of_the_core_palette_except_the_resolver()
    {
        var core = ParametersOf(typeof(DrylCommandPalette));
        var wrapper = ParametersOf(typeof(DrylAiCommandPalette));
        core.Remove(nameof(DrylCommandPalette.Resolver));   // the one thing the wrapper owns

        // Same names and same types: a parameter added to the core palette fails here
        // until the wrapper forwards it.
        Assert.Equal(core.OrderBy(p => p.Key), wrapper.OrderBy(p => p.Key));
    }

    [Fact]
    public void Forwards_the_new_parameters_to_the_inner_palette()
    {
        Services.AddScoped<ICommandResolver>(_ => new NullResolver());
        var texts = CommandPaletteTexts.Default with { Placeholder = "Befehl suchen…" };
        var items = new[] { new CommandItem { Label = "Open settings" } };
        Func<string, Task<CommandItem[]>> provider = _ => Task.FromResult(items);
        Func<string, CommandItem?> fallback = q => new CommandItem { Label = $"Ask: {q}" };

        var cut = Render<DrylAiCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Texts, texts)
            .Add(p => p.Items, items)
            .Add(p => p.SearchProvider, provider)
            .Add(p => p.Debounce, TimeSpan.FromMilliseconds(10))
            .Add(p => p.GroupOrder, CommandGroupOrder.AsProvided)
            .Add(p => p.Fallback, fallback)
            .Add(p => p.Ai, AiState.Thinking)
            .Add(p => p.Aura, AiAura.Aurora)
            .Add(p => p.AriaLabel, "Befehle"));

        var inner = cut.FindComponent<DrylCommandPalette>().Instance;
        Assert.Same(texts, inner.Texts);
        Assert.Same(items, inner.Items);
        Assert.Same(provider, inner.SearchProvider);
        Assert.Equal(TimeSpan.FromMilliseconds(10), inner.Debounce);
        Assert.Equal(CommandGroupOrder.AsProvided, inner.GroupOrder);
        Assert.Same(fallback, inner.Fallback);
        Assert.Equal(AiState.Thinking, inner.Ai);
        Assert.Equal(AiAura.Aurora, inner.Aura);
        Assert.Equal("Befehle", inner.AriaLabel);
        Assert.IsType<NullResolver>(inner.Resolver);
    }

    [Fact]
    public void Texts_translate_the_inner_palette()
    {
        Services.AddScoped<ICommandResolver>(_ => new NullResolver());
        var cut = Render<DrylAiCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Texts, CommandPaletteTexts.Default with { Placeholder = "Befehl suchen…" }));

        cut.WaitForAssertion(() =>
            Assert.Equal("Befehl suchen…", cut.Find("input").GetAttribute("placeholder")),
            TimeSpan.FromSeconds(2));
    }
}
