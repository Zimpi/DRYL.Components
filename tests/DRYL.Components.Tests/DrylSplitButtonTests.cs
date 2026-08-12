using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Rendering tests for <see cref="DrylSplitButton"/>, focused on the caret
/// segment: <c>UX-05</c> requires an icon-only button to be wrapped in a
/// <see cref="DrylTooltip"/> naming the same action as its accessible name,
/// and the caret must stay inside the popover anchor so the segment styling
/// (<c>.split-btn &gt; .popover-anchor .btn</c>) keeps matching it.
/// JSInterop is Loose because opening the menu wires dryl.popover / dryl.menu.
/// </summary>
public class DrylSplitButtonTests : BunitContext
{
    private const string CaretSelector = ".popover-anchor .btn";
    private const string MainSelector = ".split-btn > .btn";

    public DrylSplitButtonTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Caret_button_is_wrapped_in_a_tooltip()
    {
        var cut = Render<DrylSplitButton>(ps => ps.AddChildContent("Save"));

        var caret = cut.Find(CaretSelector);

        Assert.NotNull(caret.Closest(".tt-wrap"));
    }

    [Fact]
    public void Caret_tooltip_carries_the_default_menu_aria_label()
    {
        var cut = Render<DrylSplitButton>(ps => ps.AddChildContent("Save"));

        var wrap = cut.Find(CaretSelector).Closest(".tt-wrap");

        Assert.Equal("More actions", wrap!.GetAttribute("data-tt"));
    }

    [Fact]
    public void MenuAriaLabel_names_both_the_tooltip_and_the_caret()
    {
        var cut = Render<DrylSplitButton>(ps => ps
            .Add(p => p.MenuAriaLabel, "More save options")
            .AddChildContent("Save"));

        var caret = cut.Find(CaretSelector);
        var wrap = caret.Closest(".tt-wrap");

        Assert.Equal("More save options", wrap!.GetAttribute("data-tt"));
        Assert.Equal("More save options", caret.GetAttribute("aria-label"));
    }

    [Fact]
    public void Caret_stays_a_descendant_of_the_popover_anchor()
    {
        var cut = Render<DrylSplitButton>(ps => ps.AddChildContent("Save"));

        // The two selectors dryl.css uses for the caret segment: a child
        // selector onto the anchor, and a descendant selector onto the button.
        Assert.Single(cut.FindAll(".split-btn > .popover-anchor"));
        Assert.Single(cut.FindAll(".split-btn > .popover-anchor .btn"));

        // The exact nesting dryl.js relies on: focusTrigger resolves the caret
        // with ".popover-trigger button:not([disabled])", so hoisting the
        // tooltip or the button out of the trigger element breaks focus return
        // while leaving the two style selectors above green.
        Assert.Single(cut.FindAll(".popover-trigger > .tt-wrap > .btn"));
    }

    [Fact]
    public void Clicking_the_caret_opens_the_menu_through_the_tooltip_wrapper()
    {
        var cut = Render<DrylSplitButton>(ps => ps
            .AddChildContent("Save")
            .Add<DrylMenuItem>(p => p.MenuItems, ip => ip.AddChildContent("Save & close")));

        Assert.Empty(cut.FindAll("[role=menuitem]"));

        cut.Find(CaretSelector).Click();

        // The click has to bubble from the button, through the tooltip's span,
        // to the @onclick on .popover-trigger.
        Assert.Contains(cut.FindAll("[role=menuitem]"),
            i => i.TextContent.Contains("Save & close"));
    }

    // ── AI mode: one control, one effective state on both segments ───────────
    // The aura classes are written by AiAuraCss.Append into DrylButton's CssClass:
    // "ai-aura" plus "ai-thinking" / "ai-streaming", and "ai-aura--aurora" for the
    // Aurora variant. Asserting on those is asserting on what the stylesheet reads.

    [Fact]
    public void Ai_outside_a_scope_lights_both_segments()
    {
        var cut = Render<DrylSplitButton>(ps => ps
            .Add(p => p.Ai, AiState.Thinking)
            .AddChildContent("Save"));

        foreach (var segment in new[] { cut.Find(MainSelector), cut.Find(CaretSelector) })
        {
            Assert.Contains("ai-aura", segment.ClassList);
            Assert.Contains("ai-thinking", segment.ClassList);
        }
    }

    [Fact]
    public void A_scope_lights_both_segments_when_Ai_is_unset()
    {
        var cut = Render<DrylAiScope>(ps => ps
            .Add(p => p.State, AiState.Streaming)
            .AddChildContent<DrylSplitButton>(sp => sp.AddChildContent("Save")));

        foreach (var segment in new[] { cut.Find(MainSelector), cut.Find(CaretSelector) })
        {
            Assert.Contains("ai-aura", segment.ClassList);
            Assert.Contains("ai-streaming", segment.ClassList);
        }
    }

    [Fact]
    public void An_explicit_Ai_beats_the_scope_on_both_segments()
    {
        // The regression test for the deviation: the state is resolved once, by the
        // split button, and handed to both segments — rather than twice, by two
        // DrylButtons resolving their own inputs against the same scope.
        var cut = Render<DrylAiScope>(ps => ps
            .Add(p => p.State, AiState.Streaming)
            .AddChildContent<DrylSplitButton>(sp => sp
                .Add(p => p.Ai, AiState.Thinking)
                .AddChildContent("Save")));

        foreach (var segment in new[] { cut.Find(MainSelector), cut.Find(CaretSelector) })
        {
            Assert.Contains("ai-thinking", segment.ClassList);
            Assert.DoesNotContain("ai-streaming", segment.ClassList);
        }
    }

    [Fact]
    public void An_explicit_Aura_reaches_both_segments()
    {
        var cut = Render<DrylSplitButton>(ps => ps
            .Add(p => p.Ai, AiState.Thinking)
            .Add(p => p.Aura, AiAura.Aurora)
            .AddChildContent("Save"));

        foreach (var segment in new[] { cut.Find(MainSelector), cut.Find(CaretSelector) })
            Assert.Contains("ai-aura--aurora", segment.ClassList);
    }

    [Fact]
    public void Without_an_Aura_both_segments_take_the_Comet_default()
    {
        // Comet is the default variant and writes no class of its own — the Aurora
        // marker being absent is what "not pinned to Aurora" looks like in the DOM.
        var cut = Render<DrylSplitButton>(ps => ps
            .Add(p => p.Ai, AiState.Thinking)
            .AddChildContent("Save"));

        foreach (var segment in new[] { cut.Find(MainSelector), cut.Find(CaretSelector) })
        {
            Assert.Contains("ai-aura", segment.ClassList);
            Assert.DoesNotContain("ai-aura--aurora", segment.ClassList);
        }
    }

    [Fact]
    public void An_explicit_Aura_beats_the_scopes_aura_on_both_segments()
    {
        var cut = Render<DrylAiScope>(ps => ps
            .Add(p => p.State, AiState.Thinking)
            .Add(p => p.Aura, AiAura.Comet)
            .AddChildContent<DrylSplitButton>(sp => sp
                .Add(p => p.Aura, AiAura.Aurora)
                .AddChildContent("Save")));

        foreach (var segment in new[] { cut.Find(MainSelector), cut.Find(CaretSelector) })
            Assert.Contains("ai-aura--aurora", segment.ClassList);
    }

    [Fact]
    public void A_split_button_with_no_Ai_stays_plain_on_both_segments()
    {
        // AI-03: the parameter is an opt-in, so an unconfigured split button is an
        // ordinary control — on the caret as much as on the main button.
        var cut = Render<DrylSplitButton>(ps => ps.AddChildContent("Save"));

        foreach (var segment in new[] { cut.Find(MainSelector), cut.Find(CaretSelector) })
        {
            Assert.DoesNotContain("ai-aura", segment.ClassList);
            Assert.DoesNotContain("ai-thinking", segment.ClassList);
        }
    }
}
