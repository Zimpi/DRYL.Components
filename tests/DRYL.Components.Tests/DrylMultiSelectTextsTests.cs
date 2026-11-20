using Bunit;
using DRYL.Components;
using Microsoft.AspNetCore.Components.Web;

namespace DRYL.Components.Tests;

/// <summary>
/// The "Texts and announcements" criteria of <c>specs/E8 Inputs/F6 DrylMultiSelect.md</c>
/// (idea I51): every word the field renders comes from <see cref="MultiSelectTexts"/>, and
/// the polite live region says what a marked chip, a removed value and a query did.
/// JSInterop is Loose for the same reason as in <see cref="DrylMultiSelectSearchTests"/>.
/// </summary>
public class DrylMultiSelectTextsTests : BunitContext
{
    private static readonly SelectItem[] Skills =
    [
        new("blazor", "Blazor"),
        new("css", "CSS"),
        new("redis", "Redis"),
        new("rest", "REST"),
        new("docker", "Docker"),
    ];

    private static readonly MultiSelectTexts German = MultiSelectTexts.Default with
    {
        Placeholder = "Optionen wählen…",
        SearchPlaceholder = "Suchen…",
        EmptyText = "Keine Treffer",
        NoOptions = "Keine Optionen",
        ListLabel = label => string.IsNullOrEmpty(label) ? "Optionen" : $"Optionen für {label}",
        Remove = label => $"{label} entfernen",
        RemoveArmed = label => $"{label}. Rücktaste erneut drücken zum Entfernen.",
        Removed = label => $"{label} entfernt.",
        Results = count => count == 1 ? "1 Treffer" : $"{count} Treffer",
    };

    public DrylMultiSelectTextsTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private IRenderedComponent<DrylMultiSelect> RenderField(
        IReadOnlyList<string>? values = null,
        bool searchable = true,
        Action<ComponentParameterCollectionBuilder<DrylMultiSelect>>? extra = null)
    {
        IRenderedComponent<DrylMultiSelect>? cut = null;
        cut = Render<DrylMultiSelect>(ps =>
        {
            ps.Add(p => p.Items, Skills)
              .Add(p => p.Label, "Skills")
              .Add(p => p.Searchable, searchable)
              .Add(p => p.SelectedValues, values ?? [])
              .Add(p => p.SelectedValuesChanged, v => cut!.Render(r => r.Add(p => p.SelectedValues, v)));
            extra?.Invoke(ps);
        });
        return cut;
    }

    private static void Type(IRenderedComponent<DrylMultiSelect> cut, string text) =>
        cut.InvokeAsync(() => cut.Find("input.multiselect-search").Input(text)).GetAwaiter().GetResult();

    private static void Press(IRenderedComponent<DrylMultiSelect> cut, string key) =>
        cut.InvokeAsync(() => cut.Find("input.multiselect-search").KeyDown(new KeyboardEventArgs { Key = key })).GetAwaiter().GetResult();

    /// <summary>The list is only in the markup while the popover is open.</summary>
    private static IRenderedComponent<DrylMultiSelect> Open(IRenderedComponent<DrylMultiSelect> cut)
    {
        cut.InvokeAsync(() => cut.Find(".multiselect-trigger").Click()).GetAwaiter().GetResult();
        return cut;
    }

    private static string Live(IRenderedComponent<DrylMultiSelect> cut) =>
        cut.Find(".field > .visually-hidden[aria-live=polite]").TextContent;

    // ── Announcements ──────────────────────────────────────────────────

    [Fact]
    public void The_live_region_is_always_rendered_and_starts_empty()
    {
        Assert.Equal(string.Empty, Live(RenderField()));
        Assert.Equal(string.Empty, Live(RenderField(searchable: false)));
    }

    [Fact]
    public void Marking_the_last_chip_is_announced_and_removing_it_too()
    {
        var cut = RenderField(["blazor", "redis"]);

        Press(cut, "Backspace");
        Assert.Equal("Redis. Press Backspace again to remove.", Live(cut));

        Press(cut, "Backspace");
        Assert.Equal("Redis removed.", Live(cut));
    }

    [Fact]
    public void A_mark_on_the_overflow_marker_names_the_value_that_will_go()
    {
        var cut = RenderField(["blazor", "css", "redis"], extra: ps => ps.Add(p => p.MaxVisibleChips, 2));

        Press(cut, "Backspace");
        Assert.Equal("Redis. Press Backspace again to remove.", Live(cut));
    }

    [Fact]
    public void Taking_the_mark_off_clears_its_announcement_so_a_second_mark_is_heard_again()
    {
        var cut = RenderField(["blazor"]);

        Press(cut, "Backspace");
        Press(cut, "ArrowLeft");
        Assert.Equal(string.Empty, Live(cut));

        Press(cut, "Backspace");
        Assert.Equal("Blazor. Press Backspace again to remove.", Live(cut));
    }

    [Fact]
    public void A_chip_remove_button_announces_the_removal_without_search_too()
    {
        var cut = RenderField(["blazor", "css"], searchable: false);

        cut.InvokeAsync(() => cut.Find("button[aria-label='Remove CSS']").Click()).GetAwaiter().GetResult();

        Assert.Equal("CSS removed.", Live(cut));
    }

    [Fact]
    public void A_picked_value_without_an_option_is_announced_by_its_raw_value()
    {
        var cut = RenderField(["ghost"]);

        Press(cut, "Backspace");
        Assert.Equal("ghost. Press Backspace again to remove.", Live(cut));
    }

    [Fact]
    public void A_query_announces_how_many_options_it_lists()
    {
        var cut = RenderField();

        Type(cut, "re");
        Assert.Equal("2 results", Live(cut));

        Type(cut, "red");
        Assert.Equal("1 result", Live(cut));

        Type(cut, "");
        Assert.Equal(string.Empty, Live(cut));
    }

    [Fact]
    public void A_query_without_matches_leaves_the_announcement_to_the_empty_row()
    {
        var cut = RenderField();

        Type(cut, "xyz");

        Assert.Equal(string.Empty, Live(cut));
        Assert.Equal("polite", cut.Find(".select-empty").GetAttribute("aria-live"));
        Assert.Equal("No results", cut.Find(".select-empty").TextContent);
    }

    [Fact]
    public void Closing_the_list_clears_the_announcement()
    {
        var cut = RenderField();

        Type(cut, "re");
        Press(cut, "Tab");

        Assert.Equal(string.Empty, Live(cut));
    }

    [Fact]
    public void Toggling_an_option_clears_the_announcement()
    {
        var cut = RenderField();

        Type(cut, "red");
        Press(cut, "Enter");

        Assert.Equal(string.Empty, Live(cut));
    }

    // ── Texts ──────────────────────────────────────────────────────────

    [Fact]
    public void The_defaults_are_the_English_words_the_field_always_had()
    {
        var plain = RenderField(searchable: false);
        Assert.Equal("Select options…", plain.Find(".multiselect-placeholder").TextContent);
        Open(plain);
        Assert.Equal("Skills options", plain.Find("[role=listbox]").GetAttribute("aria-label"));

        var search = RenderField(["blazor"]);
        Assert.Equal("Search…", search.Find("input.multiselect-search").GetAttribute("placeholder"));
        Assert.NotNull(search.Find("button[aria-label='Remove Blazor']"));
    }

    [Fact]
    public void A_list_without_a_label_or_without_items_reads_from_Texts()
    {
        var cut = Open(Render<DrylMultiSelect>(ps => ps
            .Add(p => p.Items, [])
            .Add(p => p.AriaLabel, "Skills")));

        Assert.Equal("Options", cut.Find("[role=listbox]").GetAttribute("aria-label"));
        Assert.Equal("No options", cut.Find(".select-empty").TextContent);
    }

    [Fact]
    public void Texts_translates_every_rendered_word()
    {
        var plain = RenderField(searchable: false, extra: ps => ps.Add(p => p.Texts, German));
        Assert.Equal("Optionen wählen…", plain.Find(".multiselect-placeholder").TextContent);
        Open(plain);
        Assert.Equal("Optionen für Skills", plain.Find("[role=listbox]").GetAttribute("aria-label"));

        var empty = Open(Render<DrylMultiSelect>(ps => ps.Add(p => p.Items, []).Add(p => p.Texts, German)));
        Assert.Equal("Optionen", empty.Find("[role=listbox]").GetAttribute("aria-label"));
        Assert.Equal("Keine Optionen", empty.Find(".select-empty").TextContent);

        var nothingPicked = RenderField(extra: ps => ps.Add(p => p.Texts, German));
        Assert.Equal("Optionen wählen…", nothingPicked.Find("input.multiselect-search").GetAttribute("placeholder"));

        var search = RenderField(["blazor"], extra: ps => ps.Add(p => p.Texts, German));
        Assert.Equal("Suchen…", search.Find("input.multiselect-search").GetAttribute("placeholder"));
        Assert.NotNull(search.Find("button[aria-label='Blazor entfernen']"));

        Type(search, "xyz");
        Assert.Equal("Keine Treffer", search.Find(".select-empty").TextContent);
    }

    [Fact]
    public void Texts_translates_every_announcement()
    {
        var cut = RenderField(["blazor"], extra: ps => ps.Add(p => p.Texts, German));

        Type(cut, "re");
        Assert.Equal("2 Treffer", Live(cut));
        Type(cut, "red");
        Assert.Equal("1 Treffer", Live(cut));
        Type(cut, "");

        Press(cut, "Backspace");
        Assert.Equal("Blazor. Rücktaste erneut drücken zum Entfernen.", Live(cut));
        Press(cut, "Backspace");
        Assert.Equal("Blazor entfernt.", Live(cut));
    }

    [Fact]
    public void Placeholder_SearchPlaceholder_and_EmptyText_win_over_Texts()
    {
        var nothingPicked = RenderField(extra: ps => ps
            .Add(p => p.Texts, German)
            .Add(p => p.Placeholder, "Skills suchen…"));
        Assert.Equal("Skills suchen…", nothingPicked.Find("input.multiselect-search").GetAttribute("placeholder"));

        var search = RenderField(["blazor"], extra: ps => ps
            .Add(p => p.Texts, German)
            .Add(p => p.SearchPlaceholder, "Mehr…")
            .Add(p => p.EmptyText, "Nichts gefunden"));
        Assert.Equal("Mehr…", search.Find("input.multiselect-search").GetAttribute("placeholder"));

        Type(search, "xyz");
        Assert.Equal("Nichts gefunden", search.Find(".select-empty").TextContent);
    }
}
