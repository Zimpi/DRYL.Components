using Bunit;
using DRYL.Components;
using Microsoft.AspNetCore.Components.Web;

namespace DRYL.Components.Tests;

/// <summary>
/// The "Search" criteria of <c>specs/E8 Inputs/F6 DrylMultiSelect.md</c> (idea I43):
/// the chip input, filtering over mounted rows, the matched-text mark, and Backspace
/// and Escape in two steps. JSInterop is Loose because the popover, the presence and
/// the key helper wire themselves through dryl.js, which bUnit never executes; the
/// outside press is simulated by calling the popover's <see cref="DrylPopover.Close"/>.
/// </summary>
public class DrylMultiSelectSearchTests : BunitContext
{
    private static readonly SelectItem[] Skills =
    [
        new("blazor", "Blazor"),
        new("css", "CSS"),
        new("redis", "Redis"),
        new("rest", "REST"),
        new("docker", "Docker"),
    ];

    public DrylMultiSelectSearchTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private sealed class Probe
    {
        public IReadOnlyList<string> Values = [];
        public int Changes;
    }

    private IRenderedComponent<DrylMultiSelect> RenderField(
        Probe probe,
        Action<ComponentParameterCollectionBuilder<DrylMultiSelect>>? extra = null)
    {
        IRenderedComponent<DrylMultiSelect>? cut = null;
        cut = Render<DrylMultiSelect>(ps =>
        {
            ps.Add(p => p.Items, Skills)
              .Add(p => p.Label, "Skills")
              .Add(p => p.Searchable, true)
              .Add(p => p.SelectedValues, probe.Values)
              .Add(p => p.SelectedValuesChanged, v =>
              {
                  probe.Values = v;
                  probe.Changes++;
                  // Bind the new list back, as @bind-SelectedValues would.
                  cut!.Render(r => r.Add(p => p.SelectedValues, v));
              });
            extra?.Invoke(ps);
        });
        return cut;
    }

    private static void Type(IRenderedComponent<DrylMultiSelect> cut, string text) =>
        cut.InvokeAsync(() => cut.Find("input.multiselect-search").Input(text)).GetAwaiter().GetResult();

    private static void Press(IRenderedComponent<DrylMultiSelect> cut, string key) =>
        cut.InvokeAsync(() => cut.Find("input.multiselect-search").KeyDown(new KeyboardEventArgs { Key = key })).GetAwaiter().GetResult();

    private static void Click(IRenderedComponent<DrylMultiSelect> cut, string selector) =>
        cut.InvokeAsync(() => cut.Find(selector).Click()).GetAwaiter().GetResult();

    private static List<string> Listed(IRenderedComponent<DrylMultiSelect> cut) =>
        cut.FindAll(".multiselect-row:not(.is-filtered) [role=option]").Select(o => o.TextContent.Trim()).ToList();

    private static string? Highlighted(IRenderedComponent<DrylMultiSelect> cut) =>
        cut.FindAll("[role=option].is-highlighted").SingleOrDefault()?.TextContent.Trim();

    // ── Structure ──────────────────────────────────────────────────────

    [Fact]
    public void The_text_field_is_the_combobox_and_the_trigger_carries_no_role()
    {
        var cut = RenderField(new Probe());
        var input = cut.Find("input.multiselect-search");
        var trigger = cut.Find(".multiselect-trigger");

        Assert.Equal("combobox", input.GetAttribute("role"));
        Assert.Equal("list", input.GetAttribute("aria-autocomplete"));
        Assert.Equal("false", input.GetAttribute("aria-expanded"));
        Assert.NotNull(input.GetAttribute("aria-controls"));
        Assert.Null(trigger.GetAttribute("role"));
        Assert.Null(trigger.GetAttribute("tabindex"));
        Assert.Equal(input.Id, cut.Find("label").GetAttribute("for"));
    }

    [Fact]
    public void The_placeholder_switches_once_something_is_picked()
    {
        var cut = RenderField(new Probe(), ps => ps
            .Add(p => p.Placeholder, "Pick skills…")
            .Add(p => p.SearchPlaceholder, "Find more…"));
        Assert.Equal("Pick skills…", cut.Find("input.multiselect-search").GetAttribute("placeholder"));

        cut.Render(r => r.Add(p => p.SelectedValues, ["css"]));
        Assert.Equal("Find more…", cut.Find("input.multiselect-search").GetAttribute("placeholder"));
    }

    [Fact]
    public void Without_Searchable_the_trigger_stays_the_combobox()
    {
        var cut = Render<DrylMultiSelect>(ps => ps.Add(p => p.Items, Skills));
        Assert.Empty(cut.FindAll("input.multiselect-search"));
        Assert.Equal("combobox", cut.Find(".multiselect-trigger").GetAttribute("role"));
    }

    // ── Filtering ──────────────────────────────────────────────────────

    [Fact]
    public void Typing_opens_the_list_and_filters_case_insensitively_on_the_label()
    {
        var cut = RenderField(new Probe());
        Type(cut, "  re ");

        Assert.Equal("true", cut.Find("input.multiselect-search").GetAttribute("aria-expanded"));
        Assert.Equal(["Redis", "REST"], Listed(cut));
        Assert.Equal("Redis", Highlighted(cut));
    }

    [Fact]
    public void A_filtered_row_stays_mounted_inert_and_hidden()
    {
        var cut = RenderField(new Probe());
        Type(cut, "re");

        Assert.Equal(Skills.Length, cut.FindAll("[role=option]").Count);
        var filtered = cut.FindAll(".multiselect-row.is-filtered");
        Assert.Equal(3, filtered.Count);
        Assert.All(filtered, r =>
        {
            Assert.True(r.HasAttribute("inert"));
            Assert.Equal("true", r.GetAttribute("aria-hidden"));
            Assert.Equal("none", r.GetAttribute("role"));
        });
        Assert.All(cut.FindAll(".multiselect-row:not(.is-filtered)"), r => Assert.False(r.HasAttribute("inert")));
    }

    [Fact]
    public void A_whitespace_query_lists_every_option()
    {
        var cut = RenderField(new Probe());
        Type(cut, "   ");
        Assert.Equal(Skills.Length, Listed(cut).Count);
    }

    [Fact]
    public void A_SearchFunc_decides_alone_and_unmatched_labels_render_unmarked()
    {
        var calls = new List<string>();
        var cut = RenderField(new Probe(), ps => ps.Add(p => p.SearchFunc, (q, items) =>
        {
            calls.Add(q);
            return items.Where(i => i.Value.StartsWith(q, StringComparison.Ordinal));
        }));
        Type(cut, " do ");

        Assert.Equal(["do"], calls);
        Assert.Equal(["Docker"], Listed(cut));
        // "Docker" contains "do" ignoring case, so it is marked.
        Assert.Equal("Do", cut.Find("[role=option] mark.multiselect-match").TextContent);

        Type(cut, "bla");
        Assert.Equal(["Blazor"], Listed(cut));
    }

    [Fact]
    public void A_query_without_matches_shows_EmptyText_and_highlights_nothing()
    {
        var cut = RenderField(new Probe(), ps => ps.Add(p => p.EmptyText, "Keine Treffer"));
        Type(cut, "xyz");

        Assert.Empty(Listed(cut));
        Assert.Null(Highlighted(cut));
        Assert.Contains("Keine Treffer", cut.Find(".select-empty").TextContent);
        Assert.Null(cut.Find("input.multiselect-search").GetAttribute("aria-activedescendant"));
    }

    [Fact]
    public void The_matching_part_of_a_label_is_marked()
    {
        var cut = RenderField(new Probe());
        Type(cut, "ED");

        Assert.Equal(["Redis"], Listed(cut));
        var option = cut.Find(".multiselect-row:not(.is-filtered) [role=option]");
        Assert.Equal("ed", option.QuerySelector("mark.multiselect-match")!.TextContent);
        Assert.Equal("Redis", option.TextContent.Trim());

        Type(cut, "");
        Assert.Empty(cut.FindAll("mark"));
    }

    // ── Keyboard ───────────────────────────────────────────────────────

    [Fact]
    public void Arrows_move_across_listed_options_only_and_Enter_toggles_keeping_the_query()
    {
        var probe = new Probe();
        var cut = RenderField(probe);
        Type(cut, "re");
        Press(cut, "ArrowDown");
        Assert.Equal("REST", Highlighted(cut));
        Press(cut, "ArrowDown");
        Assert.Equal("REST", Highlighted(cut));

        var active = cut.Find("input.multiselect-search").GetAttribute("aria-activedescendant");
        Assert.Equal(cut.FindAll("[role=option]")[3].Id, active);

        Press(cut, "Enter");
        Assert.Equal(["rest"], probe.Values);
        Assert.Equal(1, probe.Changes);
        Assert.Equal("re", cut.Find("input.multiselect-search").GetAttribute("value"));
        Assert.Equal(["Redis", "REST"], Listed(cut));

        Press(cut, "ArrowUp");
        Press(cut, "Enter");
        Assert.Equal(["rest", "redis"], probe.Values);
    }

    [Fact]
    public void Enter_on_a_closed_control_opens_it_and_Space_toggles_nothing()
    {
        var probe = new Probe();
        var cut = RenderField(probe);
        Press(cut, "Enter");
        Assert.Equal("true", cut.Find("input.multiselect-search").GetAttribute("aria-expanded"));
        Assert.Equal("Blazor", Highlighted(cut));

        Press(cut, " ");
        Assert.Equal(0, probe.Changes);
    }

    [Fact]
    public void Escape_clears_the_query_first_and_closes_second()
    {
        var cut = RenderField(new Probe());
        Type(cut, "re");
        Press(cut, "Escape");

        var input = cut.Find("input.multiselect-search");
        Assert.Equal("", input.GetAttribute("value"));
        Assert.Equal("true", input.GetAttribute("aria-expanded"));
        Assert.Equal(Skills.Length, Listed(cut).Count);

        Press(cut, "Escape");
        Assert.Equal("false", cut.Find("input.multiselect-search").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Closing_the_list_clears_the_query()
    {
        var cut = RenderField(new Probe());
        Type(cut, "re");
        var popover = cut.FindComponent<DrylPopover>();
        cut.InvokeAsync(() => popover.Instance.Close()).GetAwaiter().GetResult();

        Assert.Equal("", cut.Find("input.multiselect-search").GetAttribute("value"));
        Assert.Equal("false", cut.Find("input.multiselect-search").GetAttribute("aria-expanded"));

        Type(cut, "d");
        Press(cut, "Tab");
        Assert.Equal("", cut.Find("input.multiselect-search").GetAttribute("value"));
    }

    [Fact]
    public void Backspace_in_an_empty_field_marks_then_removes_the_last_value()
    {
        var probe = new Probe { Values = ["blazor", "css"] };
        var cut = RenderField(probe);

        Press(cut, "Backspace");
        Assert.Equal(0, probe.Changes);
        var chips = cut.FindAll(".chip");
        Assert.Contains("chip--armed", chips[^1].ClassList);
        Assert.DoesNotContain("chip--armed", chips[0].ClassList);

        Press(cut, "Backspace");
        Assert.Equal(["blazor"], probe.Values);
        Assert.Equal(1, probe.Changes);
        Assert.Empty(cut.FindAll(".chip--armed"));
    }

    [Fact]
    public void Any_other_key_clears_the_mark_and_Backspace_with_text_removes_nothing()
    {
        var probe = new Probe { Values = ["blazor"] };
        var cut = RenderField(probe);

        Press(cut, "Backspace");
        Press(cut, "a");
        Assert.Empty(cut.FindAll(".chip--armed"));

        Type(cut, "c");
        Press(cut, "Backspace");
        Press(cut, "Backspace");
        Assert.Equal(0, probe.Changes);
    }

    [Fact]
    public void Past_MaxVisibleChips_the_overflow_marker_is_marked()
    {
        var probe = new Probe { Values = ["blazor", "css", "redis", "rest"] };
        var cut = RenderField(probe);

        Press(cut, "Backspace");
        Assert.Contains("chip--armed", cut.Find(".chip-overflow").ClassList);
        Press(cut, "Backspace");
        Assert.Equal(["blazor", "css", "redis"], probe.Values);
    }

    // ── Pointer and state ──────────────────────────────────────────────

    [Fact]
    public void A_click_on_the_trigger_opens_and_never_closes()
    {
        var cut = RenderField(new Probe());
        Click(cut, ".multiselect-trigger");
        Assert.Equal("true", cut.Find("input.multiselect-search").GetAttribute("aria-expanded"));
        Click(cut, ".multiselect-trigger");
        Assert.Equal("true", cut.Find("input.multiselect-search").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void A_disabled_control_disables_its_text_field_and_does_not_open()
    {
        var cut = RenderField(new Probe(), ps => ps.Add(p => p.Disabled, true));
        Assert.True(cut.Find("input.multiselect-search").HasAttribute("disabled"));
        Click(cut, ".multiselect-trigger");
        Assert.Equal("false", cut.Find("input.multiselect-search").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void The_search_field_attaches_keynav_with_its_own_key_list()
    {
        RenderField(new Probe());
        var call = JSInterop.Invocations["dryl.keynav.attach"].Single();
        Assert.Equal(2, call.Arguments.Count);
        Assert.Equal(["ArrowUp", "ArrowDown", "Enter"], (string[])call.Arguments[1]!);
    }
}
