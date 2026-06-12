using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Behavioural + API-freeze tests for <see cref="DrylPagination"/>. Pins the
/// normalised bindable events (<c>CurrentPageChanged</c> / <c>PageSizeChanged</c>,
/// formerly <c>OnPageChanged</c> / <c>OnPageSizeChanged</c>) and the navigation /
/// clamping behaviour.
/// </summary>
public class DrylPaginationTests : BunitContext
{
    private IRenderedComponent<DrylPagination> RenderBar(
        int currentPage = 0, int pageSize = 20, int total = 247,
        Action<int>? onPage = null, Action<int>? onSize = null) =>
        Render<DrylPagination>(ps =>
        {
            ps.Add(p => p.CurrentPage, currentPage)
              .Add(p => p.PageSize, pageSize)
              .Add(p => p.TotalCount, total);
            if (onPage is not null) ps.Add(p => p.CurrentPageChanged, onPage);
            if (onSize is not null) ps.Add(p => p.PageSizeChanged, onSize);
        });

    [Fact]
    public void Shows_range_info()
    {
        var cut = RenderBar();

        var info = cut.Find(".tbl-pagination-info").TextContent;
        Assert.Contains("Showing", info);
        Assert.Contains("247", info);
    }

    [Fact]
    public void Empty_shows_no_items()
    {
        var cut = RenderBar(total: 0);

        Assert.Contains("No items", cut.Find(".tbl-pagination-info").TextContent);
    }

    [Fact]
    public void First_and_previous_disabled_on_first_page()
    {
        var cut = RenderBar(currentPage: 0);

        Assert.True(cut.Find("button[aria-label='First page']").HasAttribute("disabled"));
        Assert.True(cut.Find("button[aria-label='Previous page']").HasAttribute("disabled"));
    }

    [Fact]
    public void Next_button_raises_CurrentPageChanged_with_next_index()
    {
        int? captured = null;
        var cut = RenderBar(currentPage: 0, onPage: p => captured = p);

        cut.Find("button[aria-label='Next page']").Click();

        Assert.Equal(1, captured);
    }

    [Fact]
    public void Last_button_clamps_to_final_page()
    {
        int? captured = null;
        // 247 items / 20 per page = 13 pages → last zero-indexed page is 12.
        var cut = RenderBar(currentPage: 0, onPage: p => captured = p);

        cut.Find("button[aria-label='Last page']").Click();

        Assert.Equal(12, captured);
    }

    [Fact]
    public void Page_number_button_raises_CurrentPageChanged()
    {
        int? captured = null;
        var cut = RenderBar(currentPage: 0, onPage: p => captured = p);

        cut.Find("button[aria-label='Page 2']").Click();

        Assert.Equal(1, captured); // "Page 2" label = zero-indexed page 1
    }

    [Fact]
    public void Changing_size_select_raises_PageSizeChanged()
    {
        int? captured = null;
        var cut = RenderBar(onSize: s => captured = s);

        cut.Find("select.select").Change("50");

        Assert.Equal(50, captured);
    }

    [Fact]
    public void Size_selector_hidden_when_ShowPageSize_false()
    {
        var cut = Render<DrylPagination>(ps => ps
            .Add(p => p.TotalCount, 84)
            .Add(p => p.PageSize, 10)
            .Add(p => p.ShowPageSize, false));

        Assert.Empty(cut.FindAll("select.select"));
    }
}
