using System.Linq.Expressions;
using Bunit;
using DRYL.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace DRYL.Components.Tests;

/// <summary>
/// Behavioural tests for the flagship stateful component <see cref="DrylTable{TItem}"/>:
/// row rendering, click-to-sort, row selection, and the normalised two-way
/// <c>Page</c> / <c>PageChanged</c> pagination (formerly the notification-only
/// <c>OnPageChanged</c>).
/// </summary>
public class DrylTableTests : BunitContext
{
    private sealed record Person(string Name, int Age);

    private static readonly List<Person> People =
    [
        new("Charlie", 30),
        new("Alice", 25),
        new("Bob", 40),
    ];

    // Two sortable columns (Name, Age) declared the way the table expects — as
    // DrylColumn<Person> children inside the Columns slot.
    private static RenderFragment TwoColumns() => builder =>
    {
        builder.OpenComponent<DrylColumn<Person>>(0);
        builder.AddComponentParameter(1, nameof(DrylColumn<Person>.Field),
            (Expression<Func<Person, object?>>)(p => p.Name));
        builder.AddComponentParameter(2, nameof(DrylColumn<Person>.Title), "Name");
        builder.AddComponentParameter(3, nameof(DrylColumn<Person>.Sortable), true);
        builder.CloseComponent();

        builder.OpenComponent<DrylColumn<Person>>(4);
        builder.AddComponentParameter(5, nameof(DrylColumn<Person>.Field),
            (Expression<Func<Person, object?>>)(p => p.Age));
        builder.AddComponentParameter(6, nameof(DrylColumn<Person>.Title), "Age");
        builder.AddComponentParameter(7, nameof(DrylColumn<Person>.Sortable), true);
        builder.CloseComponent();
    };

    private IRenderedComponent<DrylTable<Person>> RenderTable(
        Action<ComponentParameterCollectionBuilder<DrylTable<Person>>>? extra = null)
    {
        JSInterop.Mode = JSRuntimeMode.Loose; // table injects IJSRuntime
        return Render<DrylTable<Person>>(ps =>
        {
            ps.Add(p => p.Items, People)
              .Add(p => p.Columns, TwoColumns());
            extra?.Invoke(ps);
        });
    }

    private static IReadOnlyList<string> FirstColumnValues(IRenderedComponent<DrylTable<Person>> cut) =>
        cut.FindAll("tbody tr")
           .Select(tr => tr.QuerySelectorAll("td").FirstOrDefault()?.TextContent.Trim() ?? "")
           .ToList();

    [Fact]
    public void Renders_a_row_per_item()
    {
        var cut = RenderTable();

        var names = FirstColumnValues(cut);
        Assert.Equal(new[] { "Charlie", "Alice", "Bob" }, names);
    }

    [Fact]
    public void Clicking_sortable_header_sorts_ascending()
    {
        var cut = RenderTable();

        cut.FindAll(".tbl-th-clickable")[0].Click(); // Name header → ascending

        Assert.Equal(new[] { "Alice", "Bob", "Charlie" }, FirstColumnValues(cut));
    }

    [Fact]
    public void Clicking_sortable_header_twice_sorts_descending()
    {
        var cut = RenderTable();

        var nameHeader = cut.FindAll(".tbl-th-clickable")[0];
        nameHeader.Click(); // ascending
        cut.FindAll(".tbl-th-clickable")[0].Click(); // descending

        Assert.Equal(new[] { "Charlie", "Bob", "Alice" }, FirstColumnValues(cut));
    }

    [Fact]
    public void Selecting_a_row_raises_SelectedItemsChanged()
    {
        IReadOnlySet<Person>? selected = null;
        var cut = RenderTable(ps => ps
            .Add(p => p.Selectable, true)
            .Add(p => p.SelectedItemsChanged, (IReadOnlySet<Person> s) => selected = s));

        // First checkbox inside the body = the first data row's select box.
        cut.Find("tbody tr td.tbl-col-check input[type=checkbox]").Change(true);

        Assert.NotNull(selected);
        Assert.Contains(People[0], selected!);
    }

    [Fact]
    public void Navigating_pages_raises_PageChanged()
    {
        int? captured = null;
        var cut = RenderTable(ps => ps
            .Add(p => p.PageSize, 2)
            .Add(p => p.PageChanged, (int p) => captured = p));

        // PageSize 2 over 3 items → 2 pages; page 0 shows the first two rows.
        Assert.Equal(new[] { "Charlie", "Alice" }, FirstColumnValues(cut));

        cut.Find("button[aria-label='Next page']").Click();

        Assert.Equal(1, captured);
        Assert.Equal(new[] { "Bob" }, FirstColumnValues(cut));
    }

    [Fact]
    public void Externally_set_Page_selects_that_page_slice()
    {
        var cut = RenderTable(ps => ps
            .Add(p => p.PageSize, 2)
            .Add(p => p.Page, 1)); // jump straight to the second page

        Assert.Equal(new[] { "Bob" }, FirstColumnValues(cut));
    }

    // ───── AnimateReorder (view transitions) ─────

    private static IReadOnlyList<string> NameCellValues(IRenderedComponent<DrylTable<Person>> cut) =>
        cut.FindAll("tbody tr")
           .Select(tr => tr.QuerySelectorAll("td").Skip(1).FirstOrDefault()?.TextContent.Trim() ?? "")
           .ToList(); // td[0] is the reorder grip column

    [Fact]
    public void Rows_have_no_view_transition_name_by_default()
    {
        var cut = RenderTable(ps => ps.Add(p => p.Reorderable, true));

        foreach (var tr in cut.FindAll("tbody tr"))
            Assert.DoesNotContain("view-transition-name", tr.GetAttribute("style") ?? "");
    }

    [Fact]
    public void AnimateReorder_adds_a_view_transition_name_per_row()
    {
        var cut = RenderTable(ps => ps
            .Add(p => p.Reorderable, true)
            .Add(p => p.AnimateReorder, true)
            .Add(p => p.RowIdSelector, (Person p) => p.Name));

        var styles = cut.FindAll("tbody tr").Select(tr => tr.GetAttribute("style")).ToList();
        Assert.Equal(3, styles.Count);
        Assert.Contains("view-transition-name: tbl-row-Charlie", styles[0]);
        Assert.Contains("view-transition-name: tbl-row-Alice", styles[1]);
        Assert.Contains("view-transition-name: tbl-row-Bob", styles[2]);
    }

    [Fact]
    public void Row_id_is_sanitized_to_a_css_ident()
    {
        var cut = RenderTable(ps => ps
            .Add(p => p.Reorderable, true)
            .Add(p => p.AnimateReorder, true)
            .Add(p => p.RowIdSelector, (Person p) => $"{p.Name} v/2"));

        Assert.Contains("view-transition-name: tbl-row-Charlie_v_2",
            cut.FindAll("tbody tr")[0].GetAttribute("style"));
    }

    [Fact]
    public void Drag_reorder_with_AnimateReorder_still_moves_the_row()
    {
        // Loose JSInterop never invokes ApplyChange back — this exercises the
        // service's direct-apply fallback, so the mutation must still land.
        var cut = RenderTable(ps => ps
            .Add(p => p.Reorderable, true)
            .Add(p => p.AnimateReorder, true));

        cut.FindAll(".tbl-grip")[0].TriggerEvent("ondragstart", new DragEventArgs());
        cut.FindAll("tbody tr")[2].TriggerEvent("ondrop", new DragEventArgs());

        Assert.Equal(new[] { "Alice", "Bob", "Charlie" }, NameCellValues(cut));
    }

    [Fact]
    public void Sort_with_AnimateReorder_still_sorts()
    {
        var cut = RenderTable(ps => ps
            .Add(p => p.Reorderable, true)
            .Add(p => p.AnimateReorder, true));

        cut.FindAll(".tbl-th-clickable")[0].Click(); // Name → ascending

        Assert.Equal(new[] { "Alice", "Bob", "Charlie" }, NameCellValues(cut));
    }
}
