using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Rendering tests for <see cref="DrylButton"/>'s toggle state, the criteria
/// under "Toggle state" in <c>specs/E2 Actions/F1 DrylButton.md</c>.
///
/// The visual half of those criteria — that the active modifier is relative to
/// the variant it sits on — lives in <c>dryl.css</c> and is verified by eye, as
/// every rule in that stylesheet is. What is assertable here is the contract the
/// CSS hangs on: the modifier class appears exactly while <c>Pressed</c> is
/// <c>true</c>, it keeps the variant class beside it so the variant-relative
/// rules can match, and <c>aria-pressed</c> follows the same parameter.
/// </summary>
public class DrylButtonToggleTests : BunitContext
{
    private const string Active = "btn--active";

    [Theory]
    [InlineData(DrylButton.ButtonVariant.Primary, "btn-primary")]
    [InlineData(DrylButton.ButtonVariant.Bold, "btn-bold")]
    [InlineData(DrylButton.ButtonVariant.Danger, "btn-danger")]
    [InlineData(DrylButton.ButtonVariant.Secondary, "btn-secondary")]
    [InlineData(DrylButton.ButtonVariant.Ghost, "btn-ghost")]
    public void Pressed_button_carries_the_active_modifier_beside_its_variant_class(
        DrylButton.ButtonVariant variant, string variantClass)
    {
        var cut = Render<DrylButton>(ps => ps
            .Add(p => p.Variant, variant)
            .Add(p => p.Pressed, true)
            .AddChildContent("Toggle"));

        var css = cut.Find("button").GetAttribute("class")!;

        Assert.Contains(variantClass, css);
        Assert.Contains(Active, css);
    }

    [Theory]
    [InlineData(null, false, null)]
    [InlineData(false, false, "false")]
    [InlineData(true, true, "true")]
    public void The_active_modifier_and_aria_pressed_follow_Pressed(
        bool? pressed, bool expectModifier, string? expectedAria)
    {
        var cut = Render<DrylButton>(ps => ps
            .Add(p => p.Pressed, pressed)
            .AddChildContent("Toggle"));

        var button = cut.Find("button");

        Assert.Equal(expectModifier, button.GetAttribute("class")!.Contains(Active));
        Assert.Equal(expectedAria, button.GetAttribute("aria-pressed"));
    }

    [Fact]
    public void A_disabled_pressed_button_still_carries_the_modifier_and_the_disabled_attribute()
    {
        // The variant-relative rules are gated behind :not(:disabled), so the
        // markup keeps both and the stylesheet decides — the component never
        // drops the toggle state because the button happens to be inert.
        var cut = Render<DrylButton>(ps => ps
            .Add(p => p.Variant, DrylButton.ButtonVariant.Bold)
            .Add(p => p.Pressed, true)
            .Add(p => p.Disabled, true)
            .AddChildContent("Toggle"));

        var button = cut.Find("button");

        Assert.Contains(Active, button.GetAttribute("class")!);
        Assert.True(button.HasAttribute("disabled"));
    }
}
