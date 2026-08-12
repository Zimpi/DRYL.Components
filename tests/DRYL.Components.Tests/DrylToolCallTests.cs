using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Behavioural + API-freeze tests for <see cref="DrylToolCall"/>. Pins the renamed
/// opt-in parameter (<c>Ai</c>, formerly <c>State</c>) together with the obsolete
/// alias that keeps the rename from being a break, and the disclosure body that
/// stays in the DOM while collapsed.
/// </summary>
public class DrylToolCallTests : BunitContext
{
    [Fact]
    public void Ai_defaults_to_none()
    {
        var cut = Render<DrylToolCall>(ps => ps.Add(p => p.ToolName, "get_weather"));

        Assert.Equal(AiState.None, cut.Instance.Ai);
        Assert.Contains("Idle", cut.Find(".tool-call-status").TextContent);
    }

    [Fact]
    public void Ai_drives_the_status_label()
    {
        var cut = Render<DrylToolCall>(ps => ps
            .Add(p => p.ToolName, "get_weather")
            .Add(p => p.Ai, AiState.Thinking));

        Assert.Contains("Running", cut.Find(".tool-call-status").TextContent);
    }

    [Fact]
    public void Obsolete_State_alias_sets_Ai()
    {
#pragma warning disable CS0618 // the alias is exactly what this test pins
        var cut = Render<DrylToolCall>(ps => ps
            .Add(p => p.ToolName, "get_weather")
            .Add(p => p.State, AiState.Streaming));

        Assert.Equal(AiState.Streaming, cut.Instance.Ai);
        Assert.Equal(AiState.Streaming, cut.Instance.State);
#pragma warning restore CS0618
        Assert.Contains("Streaming", cut.Find(".tool-call-status").TextContent);
    }

    [Fact]
    public void Body_stays_in_the_dom_while_collapsed()
    {
        var cut = Render<DrylToolCall>(ps => ps
            .Add(p => p.ToolName, "get_weather")
            .Add(p => p.Arguments, "{\"city\":\"Berlin\"}"));

        Assert.DoesNotContain("is-open", cut.Find(".tool-call").GetAttribute("class"));
        Assert.Equal("true", cut.Find(".tool-call-body").GetAttribute("aria-hidden"));
        Assert.Contains("Berlin", cut.Find(".tool-call-body-content").TextContent);
    }

    [Fact]
    public void Collapsed_body_is_inert_so_its_buttons_leave_the_tab_order()
    {
        // The body stays in the DOM to animate, and DrylCodeBlock brings a copy button
        // with it. Without inert those buttons stay tabbable inside an aria-hidden
        // subtree — focusable but invisible, and unknown to the accessibility tree
        // (WCAG 4.1.2, UX-07).
        var cut = Render<DrylToolCall>(ps => ps
            .Add(p => p.ToolName, "get_weather")
            .Add(p => p.Arguments, "{\"city\":\"Berlin\"}")
            .Add(p => p.Result, "{\"c\":21}"));

        var body = cut.Find(".tool-call-body");
        Assert.Equal("true", body.GetAttribute("aria-hidden"));
        Assert.NotNull(body.GetAttribute("inert"));
        Assert.NotEmpty(body.QuerySelectorAll("button"));   // the copy buttons are there
    }

    [Fact]
    public void Expanded_body_is_not_inert()
    {
        var cut = Render<DrylToolCall>(ps => ps
            .Add(p => p.ToolName, "get_weather")
            .Add(p => p.Arguments, "{\"city\":\"Berlin\"}")
            .Add(p => p.DefaultExpanded, true));

        var body = cut.Find(".tool-call-body");
        Assert.Null(body.GetAttribute("inert"));
        Assert.Null(body.GetAttribute("aria-hidden"));
        Assert.Contains("is-open", cut.Find(".tool-call").GetAttribute("class"));
    }

    [Fact]
    public void Alias_still_wins_on_a_later_render()
    {
        // The alias must keep working across render cycles, not just on the first one.
        var cut = Render<DrylToolCall>(ps => ps
            .Add(p => p.ToolName, "get_weather")
            .Add(p => p.Ai, AiState.Thinking));

#pragma warning disable CS0618 // the alias is exactly what this test pins
        cut.Render(ps => ps
            .Add(p => p.ToolName, "get_weather")
            .Add(p => p.State, AiState.Generated));
#pragma warning restore CS0618

        Assert.Equal(AiState.Generated, cut.Instance.Ai);
        Assert.Contains("Done", cut.Find(".tool-call-status").TextContent);
    }

    [Fact]
    public void Toggling_the_head_opens_the_body()
    {
        var cut = Render<DrylToolCall>(ps => ps.Add(p => p.ToolName, "get_weather"));

        cut.Find("button.tool-call-head").Click();

        Assert.Contains("is-open", cut.Find(".tool-call").GetAttribute("class"));
        Assert.Equal("true", cut.Find("button.tool-call-head").GetAttribute("aria-expanded"));
        Assert.Null(cut.Find(".tool-call-body").GetAttribute("aria-hidden"));
    }
}
