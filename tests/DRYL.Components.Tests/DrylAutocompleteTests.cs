using Bunit;
using DRYL.Components;
using Microsoft.AspNetCore.Components.Web;

namespace DRYL.Components.Tests;

/// <summary>
/// Tests for <see cref="DrylAutocomplete{TItem}"/> — the filter, the status texts and the
/// opt-in creation path (<c>OnCreate</c>) of <c>specs/E8 Inputs/F5 DrylAutocomplete.md</c>.
///
/// The main case is <c>TItem = string</c>, which is how chip-like fields use it. The filter
/// runs on the thread pool, so every assertion on the list waits for it. JSInterop is Loose
/// because the popover and the presence wire themselves through dryl.js, which bUnit never
/// executes; the outside press is simulated by calling the popover's JS-invokable
/// <see cref="DrylPopover.Close"/> directly.
/// </summary>
public class DrylAutocompleteTests : BunitContext
{
    private static readonly string[] Known = ["Familie", "Gang", "Orden"];

    public DrylAutocompleteTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private sealed class Probe
    {
        public string? Bound;
        public int Changes;
        public List<string> CreateCalls { get; } = [];
    }

    private IRenderedComponent<DrylAutocomplete<string>> RenderField(
        Probe probe,
        Func<string, Task<string?>>? onCreate = null,
        Action<ComponentParameterCollectionBuilder<DrylAutocomplete<string>>>? extra = null)
    {
        return Render<DrylAutocomplete<string>>(ps =>
        {
            ps.Add(p => p.Items, Known)
              .Add(p => p.DisplayText, s => s)
              .Add(p => p.ValueChanged, v => { probe.Bound = v; probe.Changes++; });
            if (onCreate is not null)
            {
                ps.Add(p => p.OnCreate, q =>
                {
                    probe.CreateCalls.Add(q);
                    return onCreate(q);
                });
            }
            extra?.Invoke(ps);
        });
    }

    private static Func<string, Task<string?>> Echo => q => Task.FromResult<string?>(q);

    // Find and trigger run as one step on the renderer's dispatcher: the background filter
    // renders through InvokeAsync, and a render landing between Find and the event would
    // hand the event a handler id that render has just replaced.
    private static void Type(IRenderedComponent<DrylAutocomplete<string>> cut, string text) =>
        cut.InvokeAsync(() => cut.Find("input").Input(text)).GetAwaiter().GetResult();

    private static void Press(IRenderedComponent<DrylAutocomplete<string>> cut, string key) =>
        cut.InvokeAsync(() => cut.Find("input").KeyDown(new KeyboardEventArgs { Key = key })).GetAwaiter().GetResult();

    private static void Click(IRenderedComponent<DrylAutocomplete<string>> cut, string selector) =>
        cut.InvokeAsync(() => cut.Find(selector).Click()).GetAwaiter().GetResult();

    private static Task PressOutside(IRenderedComponent<DrylAutocomplete<string>> cut)
    {
        var popover = cut.FindComponent<DrylPopover>();
        return cut.InvokeAsync(() => popover.Instance.Close());
    }

    // ── Without OnCreate: the component behaves as before ─────────────

    [Fact]
    public void Without_OnCreate_no_create_option_is_rendered_and_the_empty_text_shows()
    {
        var cut = RenderField(new Probe());

        Type(cut, "Xyz");

        cut.WaitForAssertion(() => Assert.Contains("No results", cut.Find(".autocomplete-empty").TextContent));
        Assert.Empty(cut.FindAll(".autocomplete-create"));
    }

    [Fact]
    public void Without_OnCreate_Enter_without_a_highlight_binds_nothing()
    {
        var probe = new Probe();
        var cut = RenderField(probe);

        Type(cut, "Familie");
        Press(cut, "Enter");

        Assert.Null(probe.Bound);
        Assert.Equal("Familie", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public async Task Without_OnCreate_an_outside_press_resets_the_typed_text()
    {
        var cut = RenderField(new Probe());

        Type(cut, "Neu");
        await PressOutside(cut);

        Assert.Equal("", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Clicking_an_option_binds_its_item()
    {
        var probe = new Probe();
        var cut = RenderField(probe);

        Type(cut, "ga");
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".autocomplete-option")));
        Click(cut, ".autocomplete-option");

        Assert.Equal("Gang", probe.Bound);
        Assert.Equal("Gang", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Clearing_the_input_binds_null()
    {
        var probe = new Probe();
        var cut = RenderField(probe, extra: ps => ps.Add(p => p.Value, "Gang"));

        Type(cut, "");

        Assert.Null(probe.Bound);
        Assert.True(probe.Changes > 0);
    }

    // ── Status texts ───────────────────────────────────────────────────

    [Fact]
    public void EmptyText_replaces_the_default_empty_text()
    {
        var cut = RenderField(new Probe(), extra: ps => ps.Add(p => p.EmptyText, "Keine Treffer"));

        Type(cut, "Xyz");

        cut.WaitForAssertion(() => Assert.Contains("Keine Treffer", cut.Find(".autocomplete-empty").TextContent));
    }

    [Fact]
    public void LoadingText_is_shown_while_the_provider_searches()
    {
        var pending = new TaskCompletionSource<IEnumerable<string>>();
        var cut = Render<DrylAutocomplete<string>>(ps => ps
            .Add(p => p.ItemsProvider, (_, _) => pending.Task)
            .Add(p => p.DisplayText, s => s)
            .Add(p => p.LoadingText, "Suche…"));

        Type(cut, "Fa");

        cut.WaitForAssertion(() => Assert.Contains("Suche…", cut.Find(".autocomplete-loading").TextContent));
        pending.SetResult([]);
    }

    // ── The create option ──────────────────────────────────────────────

    [Fact]
    public void With_OnCreate_an_unmatched_query_renders_the_create_option_last()
    {
        var cut = RenderField(new Probe(), Echo);

        Type(cut, "Fa");

        cut.WaitForAssertion(() =>
        {
            var options = cut.FindAll("[role=option]");
            Assert.Equal(2, options.Count);
            Assert.Contains("Familie", options[0].TextContent);
            Assert.Contains("autocomplete-create", options[1].ClassList);
            Assert.Equal("Create \"Fa\"", options[1].QuerySelector(".autocomplete-create-label")!.TextContent);
        });
    }

    [Fact]
    public void The_create_option_carries_a_hidden_plus_glyph_and_its_label_as_name()
    {
        var cut = RenderField(new Probe(), Echo);

        Type(cut, "Neu");

        var create = cut.Find(".autocomplete-create");
        Assert.Equal("option", create.GetAttribute("role"));
        Assert.Equal("true", create.QuerySelector(".autocomplete-create-icon")!.GetAttribute("aria-hidden"));
        Assert.NotNull(create.QuerySelector(".autocomplete-create-icon svg"));
        Assert.Equal("Create \"Neu\"", create.TextContent.Trim());
    }

    [Fact]
    public void CreateLabel_builds_the_create_option_text_from_the_trimmed_query()
    {
        var cut = RenderField(new Probe(), Echo,
            ps => ps.Add(p => p.CreateLabel, q => $"„{q}“ anlegen"));

        Type(cut, "  Neu  ");

        Assert.Equal("„Neu“ anlegen", cut.Find(".autocomplete-create-label").TextContent);
    }

    [Fact]
    public void With_no_other_result_the_create_option_stands_alone_instead_of_the_empty_text()
    {
        var cut = RenderField(new Probe(), Echo);

        Type(cut, "Xyz");

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[role=option]")));
        Assert.Empty(cut.FindAll(".autocomplete-empty"));
        Assert.Single(cut.FindAll(".autocomplete-create"));
    }

    [Fact]
    public void An_exact_match_ignoring_case_renders_no_create_option()
    {
        var cut = RenderField(new Probe(), Echo);

        Type(cut, "FAMILIE");

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[role=option]")));
        Assert.Empty(cut.FindAll(".autocomplete-create"));
    }

    [Fact]
    public void A_whitespace_query_renders_no_create_option()
    {
        var cut = RenderField(new Probe(), Echo);

        Type(cut, "   ");

        cut.WaitForAssertion(() => Assert.Contains("No results", cut.Find(".autocomplete-empty").TextContent));
        Assert.Empty(cut.FindAll(".autocomplete-create"));
    }

    [Fact]
    public void Clicking_the_create_option_binds_what_OnCreate_returned()
    {
        var probe = new Probe();
        var cut = RenderField(probe, q => Task.FromResult<string?>(q.ToUpperInvariant()));

        Type(cut, " Neu ");
        Click(cut, ".autocomplete-create");

        Assert.Equal(["Neu"], probe.CreateCalls);
        Assert.Equal("NEU", probe.Bound);
        Assert.Equal("NEU", cut.Find("input").GetAttribute("value"));
    }

    // ── Committing a typed query ───────────────────────────────────────

    [Fact]
    public void Enter_without_a_highlight_picks_the_exact_match_instead_of_creating()
    {
        var probe = new Probe();
        var cut = RenderField(probe, Echo);

        Type(cut, " gang  ");
        Press(cut, "Enter");

        Assert.Equal("Gang", probe.Bound);
        Assert.Empty(probe.CreateCalls);
        Assert.Equal("Gang", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Enter_without_a_highlight_and_without_a_match_creates()
    {
        var probe = new Probe();
        var cut = RenderField(probe, Echo);

        Type(cut, "Neu ");
        Press(cut, "Enter");

        Assert.Equal(["Neu"], probe.CreateCalls);
        Assert.Equal("Neu", probe.Bound);
    }

    [Fact]
    public void Tab_without_a_highlight_creates()
    {
        var probe = new Probe();
        var cut = RenderField(probe, Echo);

        Type(cut, "Neu");
        Press(cut, "Tab");

        Assert.Equal(["Neu"], probe.CreateCalls);
        Assert.Equal("Neu", probe.Bound);
    }

    [Fact]
    public async Task An_outside_press_with_a_pending_query_creates()
    {
        var probe = new Probe();
        var cut = RenderField(probe, Echo);

        Type(cut, "Neu");
        await PressOutside(cut);

        Assert.Equal(["Neu"], probe.CreateCalls);
        Assert.Equal("Neu", probe.Bound);
        Assert.Equal("Neu", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public async Task An_outside_press_with_an_exact_match_picks_it()
    {
        var probe = new Probe();
        var cut = RenderField(probe, Echo);

        Type(cut, "orden");
        await PressOutside(cut);

        Assert.Equal("Orden", probe.Bound);
        Assert.Empty(probe.CreateCalls);
    }

    [Fact]
    public void Arrow_keys_reach_the_create_option_and_Enter_creates()
    {
        var probe = new Probe();
        var cut = RenderField(probe, Echo);

        Type(cut, "Fa");
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[role=option]").Count));

        Press(cut, "ArrowDown");
        Press(cut, "ArrowDown");

        var create = cut.Find(".autocomplete-create");
        Assert.Contains("is-highlighted", create.ClassList);
        Assert.Equal(create.Id, cut.Find("input").GetAttribute("aria-activedescendant"));

        Press(cut, "Enter");

        Assert.Equal(["Fa"], probe.CreateCalls);
        Assert.Equal("Fa", probe.Bound);
    }

    [Fact]
    public void The_keyboard_highlight_is_drawn_on_the_option()
    {
        var cut = RenderField(new Probe());

        Type(cut, "a");
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[role=option]").Count));
        Press(cut, "ArrowDown");

        var options = cut.FindAll("[role=option]");
        Assert.Contains("is-highlighted", options[0].ClassList);
        Assert.DoesNotContain("is-highlighted", options[1].ClassList);
    }

    [Fact]
    public void OnCreate_returning_null_after_Enter_binds_nothing_and_keeps_the_text()
    {
        var probe = new Probe();
        var cut = RenderField(probe, _ => Task.FromResult<string?>(null));

        Type(cut, "Neu");
        Press(cut, "Enter");

        Assert.Equal(["Neu"], probe.CreateCalls);
        Assert.Null(probe.Bound);
        Assert.Equal("Neu", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public async Task A_commit_that_is_already_creating_does_not_create_twice()
    {
        var probe = new Probe();
        var pending = new TaskCompletionSource<string?>();
        var cut = RenderField(probe, _ => pending.Task);

        Type(cut, "Neu");
        Press(cut, "Enter");
        await PressOutside(cut);
        await cut.InvokeAsync(() => pending.SetResult("Neu"));

        cut.WaitForAssertion(() => Assert.Equal("Neu", probe.Bound));
        Assert.Single(probe.CreateCalls);
    }

    [Fact]
    public async Task OnCreate_runs_on_the_renderer_dispatcher()
    {
        bool? onDispatcher = null;
        var probe = new Probe();
        var cut = RenderField(probe, q =>
        {
            onDispatcher = Renderer.Dispatcher.CheckAccess();
            return Task.FromResult<string?>(q);
        });

        Type(cut, "Neu");
        await PressOutside(cut);

        Assert.True(onDispatcher);
    }

    [Fact]
    public async Task With_a_provider_a_commit_on_stale_results_asks_the_provider_before_creating()
    {
        var probe = new Probe();
        var cut = Render<DrylAutocomplete<string>>(ps => ps
            .Add(p => p.ItemsProvider, (q, _) => Task.FromResult<IEnumerable<string>>(
                Known.Where(k => k.Contains(q, StringComparison.OrdinalIgnoreCase)).ToArray()))
            .Add(p => p.DisplayText, s => s)
            .Add(p => p.ValueChanged, v => probe.Bound = v)
            .Add(p => p.OnCreate, q => { probe.CreateCalls.Add(q); return Task.FromResult<string?>(q); }));

        // Enter lands inside the provider's debounce: nothing is listed for "gang" yet.
        Type(cut, "gang");
        Press(cut, "Enter");

        await Task.Yield();
        cut.WaitForAssertion(() => Assert.Equal("Gang", probe.Bound));
        Assert.Empty(probe.CreateCalls);
    }
}
