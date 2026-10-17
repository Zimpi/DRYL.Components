using AngleSharp.Dom;
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

    // ── Approval (I21) ────────────────────────────────────────────────

    private IRenderedComponent<DrylToolCall> RenderPending(
        Action? onApprove = null, Action? onDeny = null, AiState ai = AiState.Proposed)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        return Render<DrylToolCall>(ps =>
        {
            ps.Add(p => p.ToolName, "delete_customer")
              .Add(p => p.Arguments, "{\"id\":7}")
              .Add(p => p.Ai, ai);
            if (onApprove is not null) ps.Add(p => p.OnApprove, onApprove);
            if (onDeny is not null) ps.Add(p => p.OnDeny, onDeny);
        });
    }

    private static IElement? Button(IRenderedComponent<DrylToolCall> cut, string text) =>
        cut.FindAll(".tool-call-actions button").FirstOrDefault(b => b.TextContent.Trim() == text);

    [Fact]
    public void Proposed_with_callbacks_renders_approve_and_deny()
    {
        var cut = RenderPending(() => { }, () => { });

        Assert.NotNull(Button(cut, "Approve"));
        Assert.NotNull(Button(cut, "Deny"));
    }

    [Fact]
    public void Only_bound_callbacks_get_a_button()
    {
        var cut = RenderPending(onApprove: () => { });

        Assert.NotNull(Button(cut, "Approve"));
        Assert.Null(Button(cut, "Deny"));
    }

    [Fact]
    public void Proposed_without_callbacks_renders_no_buttons()
    {
        var cut = RenderPending();

        Assert.Empty(cut.FindAll(".tool-call-actions"));
    }

    [Theory]
    [InlineData(AiState.None)]
    [InlineData(AiState.Thinking)]
    [InlineData(AiState.Generated)]
    public void No_buttons_outside_proposed(AiState ai)
    {
        var cut = RenderPending(() => { }, () => { }, ai);

        Assert.Empty(cut.FindAll(".tool-call-actions button"));
    }

    [Fact]
    public void Buttons_sit_outside_the_collapsed_body()
    {
        var cut = RenderPending(() => { }, () => { });
        cut.Find("button.tool-call-head").Click();   // collapse the auto-opened body

        Assert.DoesNotContain("is-open", cut.Find(".tool-call").GetAttribute("class"));
        Assert.Empty(cut.Find(".tool-call-body").QuerySelectorAll(".tool-call-actions"));
        Assert.NotEmpty(cut.FindAll(".tool-call-actions button"));
    }

    [Fact]
    public void Approve_fires_once_and_does_not_toggle_the_body()
    {
        var approved = 0;
        var cut = RenderPending(() => approved++, () => { });
        var open = cut.Find(".tool-call").GetAttribute("class")!.Contains("is-open");

        Button(cut, "Approve")!.Click();

        Assert.Equal(1, approved);
        Assert.Equal(open, cut.Find(".tool-call").GetAttribute("class")!.Contains("is-open"));
    }

    [Fact]
    public void Deny_fires_once_and_does_not_toggle_the_body()
    {
        var denied = 0;
        var cut = RenderPending(() => { }, () => denied++);
        var open = cut.Find(".tool-call").GetAttribute("class")!.Contains("is-open");

        Button(cut, "Deny")!.Click();

        Assert.Equal(1, denied);
        Assert.Equal(open, cut.Find(".tool-call").GetAttribute("class")!.Contains("is-open"));
    }

    [Fact]
    public void Labels_are_overridable()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<DrylToolCall>(ps => ps
            .Add(p => p.ToolName, "delete_customer")
            .Add(p => p.Ai, AiState.Proposed)
            .Add(p => p.OnApprove, () => { })
            .Add(p => p.OnDeny, () => { })
            .Add(p => p.ApproveLabel, "Freigeben")
            .Add(p => p.DenyLabel, "Ablehnen"));

        Assert.NotNull(Button(cut, "Freigeben"));
        Assert.NotNull(Button(cut, "Ablehnen"));
    }

    [Fact]
    public void Label_defaults()
    {
        var cut = Render<DrylToolCall>();

        Assert.Equal("Approve", cut.Instance.ApproveLabel);
        Assert.Equal("Deny", cut.Instance.DenyLabel);
    }

    [Fact]
    public void Buttons_use_primary_and_ghost_in_small()
    {
        var cut = RenderPending(() => { }, () => { });

        var buttons = cut.FindComponents<DrylButton>();
        Assert.Contains(buttons, b => b.Instance.Variant == DrylButton.ButtonVariant.Primary
                                   && b.Instance.Size == DrylButton.ButtonSize.Small);
        Assert.Contains(buttons, b => b.Instance.Variant == DrylButton.ButtonVariant.Ghost
                                   && b.Instance.Size == DrylButton.ButtonSize.Small);
    }

    [Fact]
    public void Buttons_name_the_tool_for_assistive_tech()
    {
        var cut = RenderPending(() => { }, () => { });

        Assert.Equal("Approve delete_customer", Button(cut, "Approve")!.GetAttribute("aria-label"));
        Assert.Equal("Deny delete_customer", Button(cut, "Deny")!.GetAttribute("aria-label"));
    }

    [Fact]
    public void Buttons_follow_the_head_row_in_document_order()
    {
        var cut = RenderPending(() => { }, () => { });

        var all = cut.FindAll("button").ToList();
        var head = all.FindIndex(b => b.ClassList.Contains("tool-call-head"));
        var approve = all.FindIndex(b => b.TextContent.Trim() == "Approve");
        Assert.True(head >= 0 && approve > head);
    }

    [Fact]
    public void Deciding_returns_focus_to_the_head_row()
    {
        var cut = RenderPending(() => { }, () => { });

        Button(cut, "Approve")!.Click();

        JSInterop.VerifyFocusAsyncInvoke();
    }

    [Fact]
    public void Declined_reads_declined_at_none()
    {
        var cut = Render<DrylToolCall>(ps => ps
            .Add(p => p.ToolName, "delete_customer")
            .Add(p => p.Declined, true));

        Assert.Contains("Declined", cut.Find(".tool-call-status").TextContent);
        Assert.Contains("tool-call--declined", cut.Find(".tool-call").GetAttribute("class"));
        Assert.Empty(cut.FindComponents<DrylAlert>());
    }

    [Fact]
    public void Declined_does_not_change_a_live_state()
    {
        var cut = Render<DrylToolCall>(ps => ps
            .Add(p => p.ToolName, "delete_customer")
            .Add(p => p.Ai, AiState.Thinking)
            .Add(p => p.Declined, true));

        Assert.Contains("Running", cut.Find(".tool-call-status").TextContent);
        Assert.DoesNotContain("tool-call--declined", cut.Find(".tool-call").GetAttribute("class"));
    }

    [Fact]
    public void An_accepted_call_that_still_runs_reads_running_on_its_pill_only()
    {
        var cut = Render<DrylToolCall>(ps => ps
            .Add(p => p.ToolName, "send_invoice")
            .Add(p => p.Ai, AiState.Generated)
            .Add(p => p.Running, true));

        Assert.Contains("Running", cut.Find(".tool-call-status").TextContent);
        Assert.Equal(AiState.Thinking, cut.FindComponent<DrylAiIndicator>().Instance.State);
        Assert.Equal(AiState.Generated, cut.Instance.Ai);   // the card itself stays accepted

        cut.Render(ps => ps.Add(p => p.Running, false).Add(p => p.Result, "\"sent\""));

        Assert.Contains("Done", cut.Find(".tool-call-status").TextContent);
        Assert.Equal(AiState.Generated, cut.FindComponent<DrylAiIndicator>().Instance.State);
    }

    [Fact]
    public void Running_changes_nothing_outside_generated()
    {
        var cut = Render<DrylToolCall>(ps => ps
            .Add(p => p.ToolName, "send_invoice")
            .Add(p => p.Ai, AiState.Proposed)
            .Add(p => p.Running, true));

        Assert.Contains("Awaiting approval", cut.Find(".tool-call-status").TextContent);
        Assert.Equal(AiState.Proposed, cut.FindComponent<DrylAiIndicator>().Instance.State);
    }

    [Fact]
    public void A_pending_call_with_a_decision_opens_its_arguments_once()
    {
        var cut = RenderPending(() => { }, () => { });
        Assert.Contains("is-open", cut.Find(".tool-call").GetAttribute("class"));

        // The user closes it; staying in Proposed must not force it open again.
        cut.Find("button.tool-call-head").Click();
        cut.Render(ps => ps.Add(p => p.ToolName, "delete_customer"));
        Assert.DoesNotContain("is-open", cut.Find(".tool-call").GetAttribute("class"));
    }

    [Fact]
    public void Proposed_without_callbacks_does_not_open_the_body()
    {
        var cut = RenderPending();

        Assert.DoesNotContain("is-open", cut.Find(".tool-call").GetAttribute("class"));
    }

    [Fact]
    public void A_second_click_while_the_buttons_fade_sends_no_second_decision()
    {
        var decisions = 0;
        var cut = RenderPending(() => decisions++, () => decisions++);
        Button(cut, "Approve")!.Click();

        // The host accepted; the pair stays mounted while DrylPresence fades it out.
        cut.Render(ps => ps.Add(p => p.Ai, AiState.Generated));
        Assert.NotEmpty(cut.FindAll(".tool-call-actions button"));   // still mounted, fading
        foreach (var b in cut.FindAll(".tool-call-actions button")) b.Click();

        Assert.Equal(1, decisions);
    }

    [Fact]
    public void A_failing_focus_does_not_escape_the_click_handler()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true)
            .SetException(new Microsoft.JSInterop.JSException("Unable to focus an invalid element."));
        var approved = 0;
        var cut = Render<DrylToolCall>(ps => ps
            .Add(p => p.ToolName, "delete_customer")
            .Add(p => p.Ai, AiState.Proposed)
            .Add(p => p.OnApprove, () => approved++));

        Button(cut, "Approve")!.Click();

        Assert.Equal(1, approved);
    }
}
