using System.Linq.Expressions;
using Bunit;
using DRYL.Components;
using Microsoft.AspNetCore.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// The compact presentation of <see cref="DrylTable{TItem}"/> (spec <c>S10</c>): the
/// stylesheet alone decides whether the table is a grid or a record list, so these tests
/// guard what the markup has to carry for it — the breakpoint class, the column roles,
/// the unfold control, the sort/filter control and the explicit table roles.
/// </summary>
public class DrylTableCompactTests : BunitContext
{
    private sealed record Service(string Name, string Env, string Status, int Latency, string Region, string Owner);

    private static readonly List<Service> Services =
    [
        new("auth", "prod", "healthy", 24, "eu", "platform"),
        new("billing", "prod", "degraded", 142, "us", "payments"),
    ];

    private sealed record Col(
        Expression<Func<Service, object?>> Field,
        string Title,
        bool Primary = false,
        ColumnCompactRole Role = ColumnCompactRole.Auto,
        bool Sortable = false,
        bool Filterable = false);

    private static readonly Col[] SixColumns =
    [
        new(s => s.Name, "Name", Primary: true),
        new(s => s.Env, "Env"),
        new(s => s.Status, "Status"),
        new(s => s.Latency, "Latency"),
        new(s => s.Region, "Region"),
        new(s => s.Owner, "Owner"),
    ];

    private static RenderFragment Columns(IEnumerable<Col> cols) => builder =>
    {
        var seq = 0;
        foreach (var c in cols)
        {
            builder.OpenComponent<DrylColumn<Service>>(seq++);
            builder.AddComponentParameter(seq++, nameof(DrylColumn<Service>.Field), c.Field);
            builder.AddComponentParameter(seq++, nameof(DrylColumn<Service>.Title), c.Title);
            builder.AddComponentParameter(seq++, nameof(DrylColumn<Service>.Primary), c.Primary);
            builder.AddComponentParameter(seq++, nameof(DrylColumn<Service>.CompactRole), c.Role);
            builder.AddComponentParameter(seq++, nameof(DrylColumn<Service>.Sortable), c.Sortable);
            builder.AddComponentParameter(seq++, nameof(DrylColumn<Service>.Filterable), c.Filterable);
            builder.CloseComponent();
        }
    };

    private IRenderedComponent<DrylTable<Service>> RenderTable(
        IEnumerable<Col>? cols = null,
        Action<ComponentParameterCollectionBuilder<DrylTable<Service>>>? extra = null)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        return Render<DrylTable<Service>>(ps =>
        {
            ps.Add(p => p.Items, Services)
              .Add(p => p.Columns, Columns(cols ?? SixColumns));
            extra?.Invoke(ps);
        });
    }

    private static IReadOnlyList<string?> RolesOfFirstRecord(IRenderedComponent<DrylTable<Service>> cut) =>
        cut.Find("tbody tr.tbl-rec")
           .QuerySelectorAll("td[data-col-key]")
           .Select(td => td.GetAttribute("data-compact"))
           .ToList();

    [Fact]
    public void Is_compact_below_sm_by_default()
    {
        var cut = RenderTable();

        Assert.Contains("tbl-compact-sm", cut.Find(".tbl-root").ClassList);
    }

    [Theory]
    [InlineData(Breakpoint.Md, "tbl-compact-md")]
    [InlineData(Breakpoint.Lg, "tbl-compact-lg")]
    [InlineData(Breakpoint.Xl, "tbl-compact-xl")]
    public void CompactBelow_names_the_breakpoint(Breakpoint below, string expected)
    {
        var cut = RenderTable(extra: ps => ps.Add(p => p.CompactBelow, below));

        Assert.Contains(expected, cut.Find(".tbl-root").ClassList);
    }

    [Fact]
    public void CompactBelow_null_opts_out_entirely()
    {
        var cut = RenderTable(extra: ps => ps.Add(p => p.CompactBelow, (Breakpoint?)null));

        Assert.DoesNotContain(cut.Find(".tbl-root").ClassList, c => c.StartsWith("tbl-compact-"));
        Assert.All(cut.FindAll("td[data-col-key]"), td => Assert.False(td.HasAttribute("data-compact")));
        Assert.Empty(cut.FindAll(".tbl-expand-btn"));
        Assert.Empty(cut.FindAll(".tbl-compact-trigger"));
    }

    [Fact]
    public void Auto_roles_fill_title_trailing_and_two_meta_slots_in_column_order()
    {
        var cut = RenderTable();

        Assert.Equal(
            new[] { "title", "trailing", "meta", "meta-end", "detail", "detail" },
            RolesOfFirstRecord(cut));
    }

    [Fact]
    public void Explicit_roles_win_and_auto_columns_fill_the_rest()
    {
        var cut = RenderTable(
        [
            new(s => s.Name, "Name", Primary: true),
            new(s => s.Env, "Env", Role: ColumnCompactRole.Detail),
            new(s => s.Status, "Status", Role: ColumnCompactRole.Trailing),
            new(s => s.Latency, "Latency"),
            new(s => s.Region, "Region", Role: ColumnCompactRole.Hidden),
            new(s => s.Owner, "Owner", Role: ColumnCompactRole.Title),
        ]);

        // Owner claims the title, so the Primary column is an ordinary Auto column
        // and takes the first free slot — a meta value.
        Assert.Equal(
            new[] { "meta", "detail", "trailing", "meta-end", "hidden", "title" },
            RolesOfFirstRecord(cut));
    }

    [Fact]
    public void A_surplus_claim_falls_to_detail_instead_of_being_dropped()
    {
        var cut = RenderTable(
        [
            new(s => s.Name, "Name", Role: ColumnCompactRole.Title),
            new(s => s.Env, "Env", Role: ColumnCompactRole.Title),
            new(s => s.Status, "Status", Role: ColumnCompactRole.Trailing),
            new(s => s.Latency, "Latency", Role: ColumnCompactRole.Trailing),
        ]);

        Assert.Equal(new[] { "title", "detail", "trailing", "detail" }, RolesOfFirstRecord(cut));
    }

    [Fact]
    public void Detail_cells_carry_their_column_title_as_label()
    {
        var cut = RenderTable();

        var detail = cut.Find("tbody tr.tbl-rec td[data-compact='detail']");
        Assert.Equal("Region", detail.GetAttribute("data-label"));
    }

    [Fact]
    public void A_record_with_detail_columns_gets_a_compact_only_unfold_control()
    {
        var cut = RenderTable();

        var cell = cut.Find("tbody tr.tbl-rec td.tbl-col-expand");
        Assert.Contains("tbl-col-expand--compact", cell.ClassList);
        Assert.Equal("false", cell.QuerySelector(".tbl-expand-btn")!.GetAttribute("aria-expanded"));
    }

    [Fact]
    public void A_record_with_nothing_to_unfold_gets_no_unfold_control()
    {
        var cut = RenderTable(SixColumns.Take(4));

        Assert.Empty(cut.FindAll(".tbl-col-expand"));
        Assert.False(cut.Find(".tbl-root").HasAttribute("data-compact-tap"));
    }

    [Fact]
    public void Pressing_the_unfold_control_opens_the_record()
    {
        var cut = RenderTable();

        cut.Find("tbody tr.tbl-rec .tbl-expand-btn").Click();

        var row = cut.Find("tbody tr.tbl-rec");
        Assert.Contains("tbl-row--open", row.ClassList);
        Assert.Equal("true", row.QuerySelector(".tbl-expand-btn")!.GetAttribute("aria-expanded"));
    }

    [Fact]
    public void With_a_DetailTemplate_the_unfold_control_is_the_ordinary_expand_control()
    {
        var cut = RenderTable(extra: ps => ps.Add(p => p.DetailTemplate, s => $"detail {s.Name}"));

        Assert.DoesNotContain("tbl-col-expand--compact", cut.Find("tbody td.tbl-col-expand").ClassList);
    }

    [Fact]
    public void A_tap_unfolds_only_while_no_OnRowClick_is_set()
    {
        var free = RenderTable();
        Assert.Equal("true", free.Find(".tbl-root").GetAttribute("data-compact-tap"));

        var claimed = RenderTable(extra: ps => ps.Add(p => p.OnRowClick, (Service _) => { }));
        Assert.False(claimed.Find(".tbl-root").HasAttribute("data-compact-tap"));
    }

    [Fact]
    public void A_row_click_never_unfolds_from_dotnet()
    {
        // The tap is forwarded by the browser helper only while the table is compact;
        // a row click that reaches .NET must leave a wide table's rows alone.
        var cut = RenderTable();

        cut.Find("tbody tr.tbl-rec").Click();

        Assert.DoesNotContain("tbl-row--open", cut.Find("tbody tr.tbl-rec").ClassList);
    }

    [Fact]
    public void Sortable_columns_get_a_sort_control_without_a_toolbar()
    {
        var cut = RenderTable(
        [
            new(s => s.Name, "Name", Primary: true, Sortable: true),
            new(s => s.Latency, "Latency", Sortable: true),
        ]);

        Assert.NotNull(cut.Find(".tbl-compactbar .tbl-compact-trigger"));
        Assert.Empty(cut.FindAll(".tbl-toolbar"));
    }

    [Fact]
    public void With_a_toolbar_the_sort_control_sits_in_it()
    {
        var cut = RenderTable(
            [new(s => s.Name, "Name", Sortable: true)],
            ps => ps.Add(p => p.ShowToolbar, true));

        Assert.NotNull(cut.Find(".tbl-toolbar .tbl-compact-trigger"));
        Assert.Empty(cut.FindAll(".tbl-compactbar"));
    }

    [Fact]
    public void No_sortable_or_filterable_column_means_no_sort_control()
    {
        var cut = RenderTable();

        Assert.Empty(cut.FindAll(".tbl-compact-trigger"));
    }

    [Fact]
    public void The_panel_sorts_through_the_same_cycle_as_the_header()
    {
        var cut = RenderTable(
        [
            new(s => s.Name, "Name", Primary: true),
            new(s => s.Latency, "Latency", Sortable: true),
        ]);
        cut.Find(".tbl-compact-trigger").Click();

        string First() => cut.Find("tbody tr.tbl-rec td[data-compact='title']").TextContent.Trim();

        cut.Find(".tbl-compact-sort").Click();
        Assert.Equal("auth", First());
        Assert.Contains("sorted ascending", cut.Find(".tbl-compact-sort").GetAttribute("aria-label"));
        Assert.Equal("ascending", cut.FindAll("thead th")[1].GetAttribute("aria-sort"));

        cut.Find(".tbl-compact-sort").Click();
        Assert.Equal("billing", First());

        cut.Find(".tbl-compact-sort").Click();
        Assert.Contains("not sorted", cut.Find(".tbl-compact-sort").GetAttribute("aria-label"));
    }

    [Fact]
    public void The_panel_offers_the_filter_editor_of_a_filterable_column()
    {
        var cut = RenderTable(
        [
            new(s => s.Name, "Name", Primary: true),
            new(s => s.Env, "Env", Filterable: true),
        ]);
        cut.Find(".tbl-compact-trigger").Click();

        cut.Find(".tbl-compact-panel input").Input("zzz");

        Assert.Empty(cut.FindAll("tbody tr.tbl-rec"));
    }

    [Fact]
    public void The_trigger_reports_whether_its_panel_is_open()
    {
        var cut = RenderTable([new(s => s.Name, "Name", Sortable: true)]);
        var trigger = cut.Find(".tbl-compact-trigger");
        Assert.Equal("false", trigger.GetAttribute("aria-expanded"));

        trigger.Click();

        Assert.Equal("true", cut.Find(".tbl-compact-trigger").GetAttribute("aria-expanded"));
        Assert.Equal(
            cut.Find(".tbl-compact-trigger").GetAttribute("aria-controls"),
            cut.Find(".tbl-compact-panel").Id);
    }

    [Fact]
    public void Table_roles_are_explicit_so_they_survive_the_record_layout()
    {
        var cut = RenderTable();

        Assert.Equal("table", cut.Find("table.tbl").GetAttribute("role"));
        Assert.Equal("row", cut.Find("tbody tr.tbl-rec").GetAttribute("role"));
        Assert.All(cut.FindAll("thead th"), th => Assert.Equal("columnheader", th.GetAttribute("role")));
        Assert.All(cut.FindAll("tbody td"), td => Assert.Equal("cell", td.GetAttribute("role")));
    }

    [Fact]
    public void A_declared_width_reaches_body_cells_as_a_custom_property()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<DrylTable<Service>>(ps => ps
            .Add(p => p.Items, Services)
            .Add(p => p.Columns, builder =>
            {
                builder.OpenComponent<DrylColumn<Service>>(0);
                builder.AddComponentParameter(1, nameof(DrylColumn<Service>.Field),
                    (Expression<Func<Service, object?>>)(s => s.Name));
                builder.AddComponentParameter(2, nameof(DrylColumn<Service>.Width), "120px");
                builder.CloseComponent();
            }));

        Assert.Equal("width: 120px", cut.Find("thead th").GetAttribute("style"));
        Assert.Equal("--tbl-col-w: 120px", cut.Find("tbody td").GetAttribute("style"));
    }
}
