using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Rendering tests for <see cref="DrylSplitButton"/>, focused on the caret
/// segment: <c>UX-05</c> requires an icon-only button to be wrapped in a
/// <see cref="DrylTooltip"/> naming the same action as its accessible name,
/// and the caret must stay inside the popover anchor so the segment styling
/// (<c>.split-btn &gt; .popover-anchor .btn</c>) keeps matching it.
/// </summary>
public class DrylSplitButtonTests : BunitContext
{
    private const string CaretSelector = ".popover-anchor .btn";

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
    }
}
