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
        Assert.All(cut.FindAll(".progress-cell"), c => Assert.Contains("is-full", c.ClassList));
    }

    [Fact]
    public void Reports_zero_for_a_negative_value()
    {
        var cut = RenderBar(-5);
        Assert.Equal("0", AriaValueNow(cut));
        Assert.Empty(cut.FindAll(".progress-cell.is-lit"));
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

    // ── Glass cells (I36) ──────────────────────────────────────────────

    private static string Style(AngleSharp.Dom.IElement e) => e.GetAttribute("style") ?? string.Empty;

    [Fact]
    public void Draws_twenty_cells_on_a_percent_scale()
    {
        Assert.Equal(DrylProgress.DefaultCellCount, RenderBar(50).FindAll(".progress-cell").Count);
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(1, 1)]
    [InlineData(12, 12)]
    [InlineData(13, 20)]
    [InlineData(2.5, 20)]
    [InlineData(0, 20)]
    public void A_small_whole_max_draws_one_cell_per_unit(double max, int cells)
    {
        Assert.Equal(cells, RenderBar(1, max).FindAll(".progress-cell").Count);
    }

    [Fact]
    public void The_track_carries_the_cell_count()
    {
        Assert.Contains("--n: 5", Style(RenderBar(3, 5).Find("[role=progressbar]")));
    }

    [Fact]
    public void Cells_fill_in_order_and_the_leading_cell_is_partial()
    {
        // 47% of 20 cells = 9.4 cells: nine full, the tenth 40% filled.
        var cells = RenderBar(47).FindAll(".progress-cell");
        for (var i = 0; i < 9; i++) Assert.Contains("is-full", cells[i].ClassList);
        Assert.Contains("--f: 0.4", Style(cells[9]));
        Assert.Contains("is-lit", cells[9].ClassList);
        Assert.DoesNotContain("is-full", cells[9].ClassList);
        Assert.Contains("is-lead", cells[9].ClassList);
        Assert.DoesNotContain("is-lit", cells[10].ClassList);
        Assert.Contains("--f: 0", Style(cells[10]));
    }

    [Fact]
    public void Exactly_one_cell_leads()
    {
        Assert.Single(RenderBar(63).FindAll(".progress-cell.is-lead"));
    }

    [Fact]
    public void An_empty_bar_has_no_leading_cell()
    {
        Assert.Empty(RenderBar(0).FindAll(".progress-cell.is-lead"));
    }

    [Fact]
    public void Every_cell_carries_its_index()
    {
        var cells = RenderBar(10, 5).FindAll(".progress-cell");
        for (var i = 0; i < cells.Count; i++) Assert.Contains($"--i: {i};", Style(cells[i]));
    }

    [Fact]
    public void A_full_bar_is_complete_and_has_no_leading_cell()
    {
        var cut = RenderBar(100);
        Assert.Contains("progress--complete", cut.Find("[role=progressbar]").ClassList);
        Assert.Empty(cut.FindAll(".progress-cell.is-lead"));
    }

    [Fact]
    public void A_partial_bar_is_not_complete()
    {
        Assert.DoesNotContain("progress--complete", RenderBar(99).Find("[role=progressbar]").ClassList);
    }

    [Fact]
    public void An_indeterminate_bar_lights_no_cell_and_is_never_complete()
    {
        var cut = RenderBar(100, indeterminate: true);
        var track = cut.Find("[role=progressbar]");
        Assert.Contains("progress--indeterminate", track.ClassList);
        Assert.DoesNotContain("progress--complete", track.ClassList);
        Assert.Equal(DrylProgress.DefaultCellCount, cut.FindAll(".progress-cell").Count);
        Assert.Empty(cut.FindAll(".progress-cell.is-lit"));
        Assert.Empty(cut.FindAll(".progress-cell.is-lead"));
    }

    [Fact]
    public void A_first_render_staggers_from_the_first_cell()
    {
        var cells = RenderBar(30).FindAll(".progress-cell");
        Assert.Contains("--k: 0;", Style(cells[0]));
        Assert.Contains("--k: 5;", Style(cells[5]));
    }

    [Fact]
    public void A_rise_staggers_outward_from_the_previous_leading_edge()
    {
        var cut = RenderBar(25); // 5 cells
        cut.Render(ps => ps.Add(p => p.Value, 50d)); // 10 cells
        var cells = cut.FindAll(".progress-cell");
        Assert.Contains("--k: 0;", Style(cells[5]));
        Assert.Contains("--k: 4;", Style(cells[9]));
    }

    [Fact]
    public void A_fall_staggers_back_from_the_previous_leading_edge()
    {
        var cut = RenderBar(50); // 10 cells
        cut.Render(ps => ps.Add(p => p.Value, 25d)); // 5 cells
        var cells = cut.FindAll(".progress-cell");
        Assert.Contains("--k: 0;", Style(cells[9]));
        Assert.Contains("--k: 4;", Style(cells[5]));
    }

    [Fact]
    public void A_render_without_a_value_change_keeps_the_stagger()
    {
        var cut = RenderBar(25);
        cut.Render(ps => ps.Add(p => p.Value, 50d));
        cut.Render(ps => ps.Add(p => p.AriaLabel, "Upload"));
        Assert.Contains("--k: 0;", Style(cut.FindAll(".progress-cell")[5]));
    }

    [Theory]
    [InlineData(ProgressVariant.Accent, null)]
    [InlineData(ProgressVariant.Success, "progress--success")]
    [InlineData(ProgressVariant.Warning, "progress--warning")]
    [InlineData(ProgressVariant.Danger, "progress--danger")]
    public void Each_variant_renders_its_own_modifier(ProgressVariant variant, string? modifier)
    {
        var classes = Render<DrylProgress>(ps => ps.Add(p => p.Variant, variant))
            .Find("[role=progressbar]").ClassList;
        if (modifier is null)
            Assert.DoesNotContain(classes, c => c is "progress--success" or "progress--warning" or "progress--danger");
        else
            Assert.Contains(modifier, classes);
    }

    [Theory]
    [InlineData(ProgressSize.Small, "progress--sm")]
    [InlineData(ProgressSize.Large, "progress--lg")]
    public void Each_size_renders_its_own_modifier(ProgressSize size, string modifier)
    {
        Assert.Contains(modifier, Render<DrylProgress>(ps => ps.Add(p => p.Size, size)).Find("[role=progressbar]").ClassList);
    }

    [Fact]
    public void Medium_adds_no_size_modifier()
    {
        var classes = Render<DrylProgress>().Find("[role=progressbar]").ClassList;
        Assert.DoesNotContain(classes, c => c is "progress--sm" or "progress--lg");
    }

    [Fact]
    public void The_label_row_is_absent_without_a_caption_or_percentage()
    {
        Assert.Empty(RenderBar(40).FindAll(".progress-label-row"));
    }

    [Fact]
    public void An_indeterminate_bar_keeps_its_caption_but_not_the_percentage()
    {
        var cut = Render<DrylProgress>(ps => ps
            .Add(p => p.Indeterminate, true)
            .Add(p => p.ShowLabel, true)
            .Add(p => p.LabelText, "Preparing"));
        Assert.Contains("Preparing", cut.Find(".progress-label-row").TextContent);
        Assert.Empty(cut.FindAll(".progress-pct"));
    }

    [Fact]
    public void The_aura_host_wraps_the_track()
    {
        var cut = RenderBar(40);
        Assert.NotNull(cut.Find(".progress-wrap > [role=progressbar]"));
    }
}
