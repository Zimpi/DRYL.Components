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
}
