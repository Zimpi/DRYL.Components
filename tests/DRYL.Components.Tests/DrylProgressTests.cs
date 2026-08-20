using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Behavioural tests for <see cref="DrylProgress"/>. The point of them is that the
/// bar a sighted user sees and the value a screen reader is told are derived from the
/// same clamped number — the half nobody can check must not be the wrong one.
/// </summary>
public class DrylProgressTests : BunitContext
{
    private IRenderedComponent<DrylProgress> RenderBar(
        double value, double max = 100, bool indeterminate = false) =>
        Render<DrylProgress>(ps => ps
            .Add(p => p.Value, value)
            .Add(p => p.Max, max)
            .Add(p => p.Indeterminate, indeterminate));

    private static string? AriaValueNow(IRenderedComponent<DrylProgress> cut) =>
        cut.Find("[role=progressbar]").GetAttribute("aria-valuenow");

    [Fact]
    public void Reports_the_value_in_range()
    {
        Assert.Equal("42", AriaValueNow(RenderBar(42)));
    }

    [Fact]
    public void Reports_max_for_a_value_above_it()
    {
        // The bar draws full; it must not announce "120 of 100".
        var cut = RenderBar(120);
        Assert.Equal("100", AriaValueNow(cut));
        Assert.Contains("width: 100%", cut.Markup);
    }

    [Fact]
    public void Reports_zero_for_a_negative_value()
    {
        var cut = RenderBar(-5);
        Assert.Equal("0", AriaValueNow(cut));
        Assert.Contains("width: 0%", cut.Markup);
    }

    [Fact]
    public void Reports_zero_when_max_is_not_positive()
    {
        Assert.Equal("0", AriaValueNow(RenderBar(7, max: 0)));
    }

    [Fact]
    public void Reports_no_value_while_indeterminate()
    {
        Assert.Null(AriaValueNow(RenderBar(42, indeterminate: true)));
    }

    [Fact]
    public void Reports_max_as_the_upper_bound()
    {
        Assert.Equal("5", RenderBar(3, max: 5).Find("[role=progressbar]").GetAttribute("aria-valuemax"));
    }

    [Fact]
    public void Percentage_label_follows_the_clamped_value()
    {
        var cut = Render<DrylProgress>(ps => ps
            .Add(p => p.Value, 120d)
            .Add(p => p.ShowLabel, true));
        Assert.Contains("100%", cut.Markup);
    }
}
