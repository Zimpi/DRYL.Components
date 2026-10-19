using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>I25 — the palette as a host-agnostic shell for search results.</summary>
public class DrylCommandPaletteHostSearchTests : BunitContext
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(2);

    public DrylCommandPaletteHostSearchTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private static string[] Labels(IRenderedComponent<DrylCommandPalette> cut) =>
        cut.FindAll("[role=option] .cmd-item-label").Select(e => e.TextContent).ToArray();

    [Fact]
    public void EmptyText_defaults_to_English()
    {
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Items, Array.Empty<CommandItem>()));

        cut.WaitForAssertion(() => Assert.Equal("No results", cut.Find(".cmd-empty").TextContent), Wait);
    }

    [Fact]
    public void Texts_translate_the_chrome()
    {
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Items, Array.Empty<CommandItem>())
            .Add(p => p.Texts, CommandPaletteTexts.Default with
            {
                Placeholder = "Suchen…", EmptyText = "Nichts gefunden", AriaLabel = "Befehlspalette",
            }));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Suchen…", cut.Find(".cmd-search-input").GetAttribute("placeholder"));
            Assert.Equal("Nichts gefunden", cut.Find(".cmd-empty").TextContent);
            Assert.Equal("Befehlspalette", cut.Find("[role=dialog]").GetAttribute("aria-label"));
        }, Wait);
    }

    [Fact]
    public void Placeholder_parameter_wins_over_Texts()
    {
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Placeholder, "Eigener Platzhalter")
            .Add(p => p.Texts, CommandPaletteTexts.Default with { Placeholder = "Aus Texts" }));

        Assert.Equal("Eigener Platzhalter", cut.Find(".cmd-search-input").GetAttribute("placeholder"));
    }

    [Fact]
    public void Meta_renders_at_the_trailing_end()
    {
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Items, new[] { new CommandItem { Label = "Schutzweste", Meta = "12 im Lager" } }));

        cut.WaitForAssertion(() => Assert.Equal("12 im Lager", cut.Find(".cmd-item-meta").TextContent), Wait);
    }

    [Fact]
    public void Groups_are_alphabetical_by_default()
    {
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Items, new[]
            {
                new CommandItem { Label = "Seite", Category = "Seiten" },
                new CommandItem { Label = "Person", Category = "Kontakte" },
            }));

        cut.WaitForAssertion(() => Assert.Equal(["Person", "Seite"], Labels(cut)), Wait);
    }

    [Fact]
    public void AsProvided_keeps_the_provider_order_of_groups()
    {
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.GroupOrder, CommandGroupOrder.AsProvided)
            .Add(p => p.Items, new[]
            {
                new CommandItem { Label = "Seite", Category = "Seiten" },
                new CommandItem { Label = "Person", Category = "Kontakte" },
            }));

        cut.WaitForAssertion(() => Assert.Equal(["Seite", "Person"], Labels(cut)), Wait);
    }

    [Fact]
    public void Group_headers_name_their_group()
    {
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Items, new[] { new CommandItem { Label = "Person", Category = "Kontakte" } }));

        cut.WaitForAssertion(() =>
        {
            var group = cut.Find("[role=group]");
            var header = cut.Find($"#{group.GetAttribute("aria-labelledby")}");
            Assert.Equal("Kontakte", header.TextContent);
        }, Wait);
    }

    [Fact]
    public void Fallback_sits_last_while_other_rows_match()
    {
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Items, new[] { new CommandItem { Label = "Marco" } })
            .Add(p => p.Fallback, q => new CommandItem { Label = $"Fragen: {q}" }));

        cut.InvokeAsync(() => cut.Find(".cmd-search-input").Input("mar"));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(["Marco", "Fragen: mar"], Labels(cut));
            Assert.Contains("is-focused", cut.FindAll("[role=option]")[0].GetAttribute("class"));
        }, Wait);
    }

    [Fact]
    public void Fallback_moves_first_and_focused_when_nothing_matches()
    {
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Items, new[] { new CommandItem { Label = "Marco" } })
            .Add(p => p.Fallback, q => new CommandItem { Label = $"Fragen: {q}" }));

        cut.InvokeAsync(() => cut.Find(".cmd-search-input").Input("zzz"));

        cut.WaitForAssertion(() =>
        {
            var options = cut.FindAll("[role=option]");
            Assert.Single(options);
            Assert.Contains("Fragen: zzz", options[0].TextContent);
            Assert.Contains("is-focused", options[0].GetAttribute("class"));
        }, Wait);
    }

    [Fact]
    public void No_fallback_for_the_empty_query()
    {
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Items, new[] { new CommandItem { Label = "Marco" } })
            .Add(p => p.Fallback, q => new CommandItem { Label = $"Fragen: {q}" }));

        cut.WaitForAssertion(() => Assert.Equal(["Marco"], Labels(cut)), Wait);
    }

    [Fact]
    public void Running_an_item_fires_OnItemRun()
    {
        CommandItem? ran = null;
        var item = new CommandItem { Id = "p1", Label = "Marco", Type = CommandItemType.Action };
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Items, new[] { item })
            .Add(p => p.OnItemRun, (CommandItem i) => { ran = i; }));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[role=option]")), Wait);
        cut.InvokeAsync(() => cut.Find("[role=option]").Click());

        cut.WaitForAssertion(() => Assert.Same(item, ran), Wait);
    }

    [Fact]
    public void Running_the_fallback_fires_OnItemRun()
    {
        CommandItem? ran = null;
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Items, Array.Empty<CommandItem>())
            .Add(p => p.Fallback, q => new CommandItem { Id = "ask", Label = $"Fragen: {q}" })
            .Add(p => p.OnItemRun, (CommandItem i) => { ran = i; }));

        cut.InvokeAsync(() => cut.Find(".cmd-search-input").Input("wer"));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[role=option]")), Wait);
        cut.InvokeAsync(() => cut.Find(".cmd-search-input").KeyDown("Enter"));

        cut.WaitForAssertion(() => Assert.Equal("ask", ran?.Id), Wait);
    }

    [Fact]
    public void A_row_that_survives_a_search_keeps_its_element()
    {
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Items, new[]
            {
                new CommandItem { Id = "a", Label = "Anwesen" },
                new CommandItem { Id = "m", Label = "Marco" },
            }));

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[role=option]").Count), Wait);

        cut.InvokeAsync(() => cut.Find(".cmd-search-input").Input("mar"));

        // Keyed by Id: the row survives as the same keyed element, not as a new one.
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(["Marco"], Labels(cut));
            var row = cut.Find("[data-cmd-key='item:m']");
            Assert.DoesNotContain("is-leaving", row.GetAttribute("class"));
        }, Wait);
    }

    [Fact]
    public void An_item_without_Id_is_keyed_by_group_and_label()
    {
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Items, new[]
            {
                new CommandItem { Label = "Marco", Category = "Kontakte" },
                new CommandItem { Label = "Marco", Category = "Kontakte" },
            }));

        cut.WaitForAssertion(() =>
        {
            var keys = cut.FindAll(".cmd-row[data-cmd-key^='item:']").Select(r => r.GetAttribute("data-cmd-key")).ToArray();
            Assert.Equal(2, keys.Length);
            Assert.Equal(2, keys.Distinct().Count());
        }, Wait);
    }

    [Fact]
    public void A_dropped_row_leaves_and_is_not_an_option()
    {
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Items, new[]
            {
                new CommandItem { Id = "a", Label = "Anwesen" },
                new CommandItem { Id = "m", Label = "Marco" },
            }));

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[role=option]").Count), Wait);
        cut.InvokeAsync(() => cut.Find(".cmd-search-input").Input("mar"));

        cut.WaitForAssertion(() =>
        {
            var leaving = cut.Find("[data-cmd-key='item:a']");
            Assert.Contains("is-leaving", leaving.GetAttribute("class"));
            Assert.Empty(leaving.QuerySelectorAll("[role=option]"));
        }, Wait);

        cut.InvokeAsync(() => cut.Instance.OnRowLeft("item:a"));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-cmd-key='item:a']")), Wait);
    }

    [Fact]
    public void Tab_moves_to_the_first_row_of_the_next_group_and_wraps()
    {
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.GroupOrder, CommandGroupOrder.AsProvided)
            .Add(p => p.Items, new[]
            {
                new CommandItem { Label = "Marco", Category = "Kontakte" },
                new CommandItem { Label = "Luis", Category = "Kontakte" },
                new CommandItem { Label = "Karte", Category = "Seiten" },
            }));

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[role=option]").Count), Wait);

        string Focused() => cut.Find("[role=option].is-focused .cmd-item-label").TextContent;

        cut.InvokeAsync(() => cut.Instance.OnGroupKey(backwards: false));
        cut.WaitForAssertion(() => Assert.Equal("Karte", Focused()), Wait);

        cut.InvokeAsync(() => cut.Instance.OnGroupKey(backwards: false));
        cut.WaitForAssertion(() => Assert.Equal("Marco", Focused()), Wait);

        cut.InvokeAsync(() => cut.Instance.OnGroupKey(backwards: true));
        cut.WaitForAssertion(() => Assert.Equal("Karte", Focused()), Wait);
    }

    [Fact]
    public void The_empty_query_is_not_debounced()
    {
        var asked = new List<string>();
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Debounce, TimeSpan.FromSeconds(30))
            .Add(p => p.SearchProvider, q =>
            {
                lock (asked) asked.Add(q);
                return Task.FromResult(new[] { new CommandItem { Label = "Zuletzt" } });
            }));

        cut.WaitForAssertion(() => Assert.Equal(["Zuletzt"], Labels(cut)), Wait);
        lock (asked) Assert.Equal([""], asked);
    }

    [Fact]
    public void A_query_waits_for_the_Debounce()
    {
        var asked = new List<string>();
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Debounce, TimeSpan.FromSeconds(30))
            .Add(p => p.SearchProvider, q =>
            {
                lock (asked) asked.Add(q);
                return Task.FromResult(Array.Empty<CommandItem>());
            }));

        cut.WaitForAssertion(() => { lock (asked) Assert.Equal([""], asked); }, Wait);
        cut.InvokeAsync(() => cut.Find(".cmd-search-input").Input("mar"));
        Thread.Sleep(300);

        lock (asked) Assert.Equal([""], asked);
    }

    [Fact]
    public void The_first_load_shows_a_skeleton_instead_of_a_spinner()
    {
        var gate = new TaskCompletionSource<CommandItem[]>();
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.SearchProvider, _ => gate.Task));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".cmd-skeleton .skel-wrap")), Wait);
        Assert.Empty(cut.FindAll(".cmd-search-spinner"));

        gate.SetResult([new CommandItem { Label = "Marco" }]);
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(["Marco"], Labels(cut));
            Assert.Contains("is-leaving", cut.Find(".cmd-skeleton").GetAttribute("class"));
        }, Wait);
    }

    [Fact]
    public void A_later_query_quiets_the_previous_rows_while_it_loads()
    {
        var calls = 0;
        var gate = new TaskCompletionSource<CommandItem[]>();
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Debounce, TimeSpan.Zero)
            .Add(p => p.SearchProvider, _ => Interlocked.Increment(ref calls) == 1
                ? Task.FromResult(new[] { new CommandItem { Label = "Marco" } })
                : gate.Task));

        cut.WaitForAssertion(() => Assert.Equal(["Marco"], Labels(cut)), Wait);
        cut.InvokeAsync(() => cut.Find(".cmd-search-input").Input("lu"));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("is-stale", cut.Find(".cmd-results").GetAttribute("class"));
            Assert.Equal(["Marco"], Labels(cut));
        }, Wait);

        gate.SetResult([new CommandItem { Label = "Luis" }]);
        cut.WaitForAssertion(() =>
            Assert.DoesNotContain("is-stale", cut.Find(".cmd-results").GetAttribute("class")), Wait);
    }

    [Fact]
    public void Closing_keeps_the_overlay_mounted_while_its_exit_plays()
    {
        var cut = Render<DrylCommandPalette>(ps => ps.Add(p => p.Open, true));

        cut.Render(ps => ps.Add(p => p.Open, false));

        Assert.Contains("presence-exit", cut.Find(".cmd-layer").GetAttribute("class"));
    }
}
