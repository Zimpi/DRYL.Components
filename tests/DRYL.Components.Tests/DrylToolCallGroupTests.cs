using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Behavioural + API-freeze tests for <see cref="DrylToolCallGroup"/>. Pins the
/// renamed opt-in parameter (<c>Ai</c>, formerly <c>State</c>) with its obsolete
/// alias, the summary vocabulary, and the error auto-reveal.
/// </summary>
public class DrylToolCallGroupTests : BunitContext
{
    [Fact]
    public void Ai_defaults_to_none_and_the_row_reads_as_settled()
    {
        var cut = Render<DrylToolCallGroup>(ps => ps.Add(p => p.Count, 4));

        Assert.Equal(AiState.None, cut.Instance.Ai);
        Assert.Contains("4 tool calls", cut.Find(".tool-group-label").TextContent);
        Assert.Contains("Done", cut.Find(".tool-group-status").TextContent);
    }

    [Fact]
    public void Count_of_one_reads_singular()
    {
        var cut = Render<DrylToolCallGroup>(ps => ps.Add(p => p.Count, 1));

        Assert.Contains("1 tool call", cut.Find(".tool-group-label").TextContent);
        Assert.DoesNotContain("tool calls", cut.Find(".tool-group-label").TextContent);
    }

    [Fact]
    public void Running_group_tickers_the_active_label()
    {
        var cut = Render<DrylToolCallGroup>(ps => ps
            .Add(p => p.Count, 3)
            .Add(p => p.ActiveLabel, "update_task")
            .Add(p => p.Ai, AiState.Thinking));

        Assert.Contains("update_task", cut.Find(".tool-group-label").TextContent);
        Assert.Contains("ticker", cut.Find(".tool-group-label").GetAttribute("class"));
        Assert.Contains("Running", cut.Find(".tool-group-status").TextContent);
    }

    [Fact]
    public void Obsolete_State_alias_sets_Ai()
    {
#pragma warning disable CS0618 // the alias is exactly what this test pins
        var cut = Render<DrylToolCallGroup>(ps => ps
            .Add(p => p.Count, 2)
            .Add(p => p.State, AiState.Streaming));

        Assert.Equal(AiState.Streaming, cut.Instance.Ai);
        Assert.Equal(AiState.Streaming, cut.Instance.State);
#pragma warning restore CS0618
        Assert.Contains("Streaming", cut.Find(".tool-group-status").TextContent);
    }

    [Fact]
    public void HasError_reveals_the_body_and_reads_as_error()
    {
        var cut = Render<DrylToolCallGroup>(ps => ps
            .Add(p => p.Count, 2)
            .Add(p => p.HasError, true));

        Assert.Contains("is-open", cut.Find(".tool-group").GetAttribute("class"));
        Assert.Contains("tool-group--error", cut.Find(".tool-group").GetAttribute("class"));
        Assert.Contains("Error", cut.Find(".tool-group-status").TextContent);
    }

    [Fact]
    public void Collapsed_body_is_inert_so_the_cards_leave_the_tab_order()
    {
        // A collapsed group keeps its cards in the DOM; each card head is a button, and
        // each code block inside brings another. Without inert they all stay tabbable
        // inside an aria-hidden subtree (WCAG 4.1.2, UX-07).
        var cut = Render<DrylToolCallGroup>(ps => ps
            .Add(p => p.Count, 1)
            .AddChildContent("<button type=\"button\">inner</button>"));

        var body = cut.Find(".tool-group-body");
        Assert.Equal("true", body.GetAttribute("aria-hidden"));
        Assert.NotNull(body.GetAttribute("inert"));
        Assert.NotEmpty(body.QuerySelectorAll("button"));
    }

    [Fact]
    public void Body_stays_in_the_dom_while_collapsed()
    {
        var cut = Render<DrylToolCallGroup>(ps => ps
            .Add(p => p.Count, 1)
            .AddChildContent("<span>inner card</span>"));

        Assert.DoesNotContain("is-open", cut.Find(".tool-group").GetAttribute("class"));
        Assert.Equal("true", cut.Find(".tool-group-body").GetAttribute("aria-hidden"));
        Assert.Contains("inner card", cut.Find(".tool-group-body-content").TextContent);
    }
}
