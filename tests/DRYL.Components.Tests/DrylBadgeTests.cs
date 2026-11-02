using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Rendering tests for <see cref="DrylBadge"/>. These double as a worked
/// example of how to bUnit-test a DRYL component: render it, assert on the
/// emitted markup / CSS classes.
/// </summary>
public class DrylBadgeTests : BunitContext
{
    [Fact]
    public void Renders_child_content()
    {
        var cut = Render<DrylBadge>(ps => ps.AddChildContent("Healthy"));

        Assert.Contains("Healthy", cut.Markup);
    }

    [Fact]
    public void Default_kind_has_only_base_class()
    {
        var cut = Render<DrylBadge>(ps => ps.AddChildContent("Neutral"));

        var span = cut.Find("span");
        Assert.Equal("badge", span.GetAttribute("class"));
    }

    [Theory]
    [InlineData(DrylBadge.BadgeKind.Accent, "badge-accent")]
    [InlineData(DrylBadge.BadgeKind.Success, "badge-success")]
    [InlineData(DrylBadge.BadgeKind.Warning, "badge-warning")]
    [InlineData(DrylBadge.BadgeKind.Danger, "badge-danger")]
    public void Kind_maps_to_modifier_class(DrylBadge.BadgeKind kind, string expected)
    {
        var cut = Render<DrylBadge>(ps => ps
            .Add(p => p.Kind, kind)
            .AddChildContent("x"));

        Assert.Contains(expected, cut.Find("span").GetAttribute("class"));
    }

    [Fact]
    public void Dot_adds_dot_modifier()
    {
        var cut = Render<DrylBadge>(ps => ps
            .Add(p => p.Dot, true)
            .AddChildContent("live"));

        Assert.Contains("badge-dot", cut.Find("span").GetAttribute("class"));
    }

    [Fact]
    public void Forwards_unmatched_attributes()
    {
        var cut = Render<DrylBadge>(ps => ps
            .AddChildContent("x")
            .AddUnmatched("data-test", "yes"));

        Assert.Equal("yes", cut.Find("span").GetAttribute("data-test"));
    }

    // ── I40: Puls & Zähler ────────────────────────────────────────────

    [Fact]
    public void Child_content_sits_in_the_label()
    {
        var cut = Render<DrylBadge>(ps => ps.AddChildContent("Healthy"));

        Assert.Equal("Healthy", cut.Find(".badge > .badge-label").TextContent);
    }

    [Fact]
    public void First_render_carries_no_kind_change_class()
    {
        var cut = Render<DrylBadge>(ps => ps.Add(p => p.Kind, DrylBadge.BadgeKind.Success).AddChildContent("x"));

        var css = cut.Find(".badge").GetAttribute("class")!;
        Assert.DoesNotContain("badge-kind-a", css);
        Assert.DoesNotContain("badge-kind-b", css);
    }

    [Fact]
    public void Each_kind_change_alternates_the_change_class()
    {
        var cut = Render<DrylBadge>(ps => ps.Add(p => p.Kind, DrylBadge.BadgeKind.Success).AddChildContent("x"));

        cut.Render(ps => ps.Add(p => p.Kind, DrylBadge.BadgeKind.Danger));
        Assert.Contains("badge-kind-a", cut.Find(".badge").GetAttribute("class"));

        cut.Render(ps => ps.Add(p => p.Kind, DrylBadge.BadgeKind.Warning));
        var css = cut.Find(".badge").GetAttribute("class")!;
        Assert.Contains("badge-kind-b", css);
        Assert.DoesNotContain("badge-kind-a", css);
    }

    [Fact]
    public void Rerender_with_the_same_kind_does_not_restart_the_change()
    {
        var cut = Render<DrylBadge>(ps => ps.Add(p => p.Kind, DrylBadge.BadgeKind.Success).AddChildContent("x"));
        cut.Render(ps => ps.Add(p => p.Kind, DrylBadge.BadgeKind.Danger));
        cut.Render(ps => ps.Add(p => p.Kind, DrylBadge.BadgeKind.Danger));

        Assert.Contains("badge-kind-a", cut.Find(".badge").GetAttribute("class"));
    }

    [Fact]
    public void Value_takes_precedence_over_child_content()
    {
        var cut = Render<DrylBadge>(ps => ps.Add(p => p.Value, "12").AddChildContent("ignored"));

        Assert.DoesNotContain("ignored", cut.Markup);
        Assert.Equal("12", cut.Find(".visually-hidden").TextContent);
    }

    [Fact]
    public void Value_label_is_hidden_from_assistive_tech_and_sized_by_length()
    {
        var cut = Render<DrylBadge>(ps => ps.Add(p => p.Value, "v3.16.0"));

        var label = cut.Find(".badge-label-value");
        Assert.Equal("true", label.GetAttribute("aria-hidden"));
        Assert.Contains("--badge-len: 7", label.GetAttribute("style"));
        Assert.Equal(7, cut.FindAll(".badge-char").Count);
    }

    [Fact]
    public void A_growing_number_rolls_only_the_changed_digits_up()
    {
        var cut = Render<DrylBadge>(ps => ps.Add(p => p.Value, "128"));
        cut.Render(ps => ps.Add(p => p.Value, "131"));

        var digits = cut.FindAll(".badge-digit");
        Assert.Equal(3, digits.Count);
        Assert.DoesNotContain("badge-digit-up", digits[0].GetAttribute("class"));
        Assert.Contains("badge-digit-up", digits[1].GetAttribute("class"));
        Assert.Contains("badge-digit-up", digits[2].GetAttribute("class"));
        Assert.Equal("2", digits[1].QuerySelector(".badge-digit-old")!.TextContent);
        Assert.Equal("131", cut.Find(".visually-hidden").TextContent);
    }

    [Fact]
    public void A_shrinking_number_rolls_down()
    {
        var cut = Render<DrylBadge>(ps => ps.Add(p => p.Value, "10"));
        cut.Render(ps => ps.Add(p => p.Value, "9"));

        var digits = cut.FindAll(".badge-digit");
        Assert.Single(digits);
        Assert.Contains("badge-digit-down", digits[0].GetAttribute("class"));
        Assert.Contains("--badge-len: 1", cut.Find(".badge-label-value").GetAttribute("style"));
    }

    [Fact]
    public void A_word_change_lets_the_old_word_leave_and_the_new_rise()
    {
        var cut = Render<DrylBadge>(ps => ps.Add(p => p.Value, "Healthy"));
        cut.Render(ps => ps.Add(p => p.Value, "Down"));

        Assert.Equal("Down", cut.Find(".badge-word-in").TextContent);
        Assert.Equal("Healthy", cut.Find(".badge-word-out").TextContent);
        Assert.Empty(cut.FindAll(".badge-digit"));
    }

    [Fact]
    public void Rerender_with_the_same_value_starts_no_animation()
    {
        var cut = Render<DrylBadge>(ps => ps.Add(p => p.Value, "Healthy"));
        cut.Render(ps => ps.Add(p => p.Value, "Healthy"));

        Assert.Empty(cut.FindAll(".badge-word-in"));
        Assert.Empty(cut.FindAll(".badge-word-out"));
    }

    [Fact]
    public void Numbers_are_parsed_invariantly()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
        try
        {
            var cut = Render<DrylBadge>(ps => ps.Add(p => p.Value, "1.5"));
            cut.Render(ps => ps.Add(p => p.Value, "1.7"));

            Assert.Contains("badge-digit-up", cut.FindAll(".badge-digit")[2].GetAttribute("class"));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }
}
