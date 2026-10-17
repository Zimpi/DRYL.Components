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

    // ── Approval (I21) ────────────────────────────────────────────────

    private IRenderedComponent<DrylToolCallGroup> RenderPending(
        int pending, AiState ai = AiState.Proposed, Action? onApproveAll = null, Action? onDenyAll = null)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        return Render<DrylToolCallGroup>(ps =>
        {
            ps.Add(p => p.Count, 3).Add(p => p.PendingCount, pending).Add(p => p.Ai, ai);
            ps.Add(p => p.OnApproveAll, onApproveAll ?? (() => { }));
            ps.Add(p => p.OnDenyAll, onDenyAll ?? (() => { }));
        });
    }

    private static AngleSharp.Dom.IElement? Bulk(IRenderedComponent<DrylToolCallGroup> cut, string text) =>
        cut.FindAll(".tool-group-actions button").FirstOrDefault(b => b.TextContent.Trim() == text);

    [Fact]
    public void Proposed_reads_awaiting_approval()
    {
        var cut = RenderPending(2);

        Assert.Equal("2 awaiting approval", cut.Find(".tool-group-label").TextContent.Trim());
        Assert.Contains("Awaiting approval", cut.Find(".tool-group-status").TextContent);
    }

    [Fact]
    public void Proposed_without_pending_count_falls_back_to_the_count()
    {
        var cut = RenderPending(0);

        Assert.Equal("3 tool calls", cut.Find(".tool-group-label").TextContent.Trim());
    }

    [Fact]
    public void Proposed_outranks_error_in_the_status_pill()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<DrylToolCallGroup>(ps => ps
            .Add(p => p.Count, 2).Add(p => p.PendingCount, 1)
            .Add(p => p.Ai, AiState.Proposed).Add(p => p.HasError, true));

        Assert.Contains("Awaiting approval", cut.Find(".tool-group-status").TextContent);
    }

    [Fact]
    public void Two_pending_render_both_bulk_buttons()
    {
        var cut = RenderPending(2);

        Assert.NotNull(Bulk(cut, "Approve all"));
        Assert.NotNull(Bulk(cut, "Deny all"));
    }

    [Fact]
    public void One_pending_renders_no_bulk_buttons()
    {
        var cut = RenderPending(1);

        Assert.Empty(cut.FindAll(".tool-group-actions button"));
    }

    [Theory]
    [InlineData(AiState.None)]
    [InlineData(AiState.Thinking)]
    public void No_bulk_buttons_outside_proposed(AiState ai)
    {
        var cut = RenderPending(3, ai);

        Assert.Empty(cut.FindAll(".tool-group-actions button"));
    }

    [Fact]
    public void Unbound_bulk_callbacks_get_no_button()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<DrylToolCallGroup>(ps => ps
            .Add(p => p.Count, 3).Add(p => p.PendingCount, 3).Add(p => p.Ai, AiState.Proposed)
            .Add(p => p.OnDenyAll, () => { }));

        Assert.Null(Bulk(cut, "Approve all"));
        Assert.NotNull(Bulk(cut, "Deny all"));
    }

    [Fact]
    public void Bulk_buttons_sit_outside_the_body()
    {
        var cut = RenderPending(2);

        Assert.Empty(cut.Find(".tool-group-body").QuerySelectorAll(".tool-group-actions"));
    }

    [Fact]
    public void Approve_all_fires_once_and_does_not_toggle()
    {
        var approved = 0;
        var cut = RenderPending(2, onApproveAll: () => approved++);
        var before = cut.Find(".tool-group").GetAttribute("class");

        Bulk(cut, "Approve all")!.Click();

        Assert.Equal(1, approved);
        Assert.Equal(before!.Contains("is-open"), cut.Find(".tool-group").GetAttribute("class")!.Contains("is-open"));
        JSInterop.VerifyFocusAsyncInvoke();
    }

    [Fact]
    public void Deny_all_fires_once()
    {
        var denied = 0;
        var cut = RenderPending(2, onDenyAll: () => denied++);

        Bulk(cut, "Deny all")!.Click();

        Assert.Equal(1, denied);
    }

    [Fact]
    public void Bulk_labels_default_and_override()
    {
        var plain = Render<DrylToolCallGroup>();
        Assert.Equal("Approve all", plain.Instance.ApproveAllLabel);
        Assert.Equal("Deny all", plain.Instance.DenyAllLabel);

        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<DrylToolCallGroup>(ps => ps
            .Add(p => p.PendingCount, 2).Add(p => p.Ai, AiState.Proposed)
            .Add(p => p.OnApproveAll, () => { }).Add(p => p.OnDenyAll, () => { })
            .Add(p => p.ApproveAllLabel, "Alle freigeben").Add(p => p.DenyAllLabel, "Alle ablehnen"));
        Assert.NotNull(Bulk(cut, "Alle freigeben"));
        Assert.NotNull(Bulk(cut, "Alle ablehnen"));
    }

    [Fact]
    public void Bulk_buttons_use_primary_and_ghost_in_small()
    {
        var cut = RenderPending(2);

        var buttons = cut.FindComponents<DrylButton>();
        Assert.Contains(buttons, b => b.Instance.Variant == DrylButton.ButtonVariant.Primary
                                   && b.Instance.Size == DrylButton.ButtonSize.Small);
        Assert.Contains(buttons, b => b.Instance.Variant == DrylButton.ButtonVariant.Ghost
                                   && b.Instance.Size == DrylButton.ButtonSize.Small);
    }

    [Fact]
    public void Entering_proposed_opens_the_group_once()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<DrylToolCallGroup>(ps => ps.Add(p => p.Count, 2).Add(p => p.Ai, AiState.Thinking));
        Assert.DoesNotContain("is-open", cut.Find(".tool-group").GetAttribute("class"));

        cut.Render(ps => ps.Add(p => p.Count, 2).Add(p => p.PendingCount, 2).Add(p => p.Ai, AiState.Proposed));
        Assert.Contains("is-open", cut.Find(".tool-group").GetAttribute("class"));

        // The user closes it; staying in Proposed must not force it open again.
        cut.Find("button.tool-group-head").Click();
        cut.Render(ps => ps.Add(p => p.Count, 2).Add(p => p.PendingCount, 1).Add(p => p.Ai, AiState.Proposed));
        Assert.DoesNotContain("is-open", cut.Find(".tool-group").GetAttribute("class"));
    }

    [Fact]
    public void Re_entering_proposed_opens_the_group_again()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<DrylToolCallGroup>(ps => ps.Add(p => p.Count, 2).Add(p => p.PendingCount, 2).Add(p => p.Ai, AiState.Proposed));
        cut.Find("button.tool-group-head").Click();   // closed by the user

        cut.Render(ps => ps.Add(p => p.Ai, AiState.Thinking).Add(p => p.PendingCount, 0));
        cut.Render(ps => ps.Add(p => p.Ai, AiState.Proposed).Add(p => p.PendingCount, 1));

        Assert.Contains("is-open", cut.Find(".tool-group").GetAttribute("class"));
    }

    [Fact]
    public void A_second_bulk_click_while_the_buttons_fade_sends_no_second_decision()
    {
        var decisions = 0;
        var cut = RenderPending(2, onApproveAll: () => decisions++, onDenyAll: () => decisions++);
        Bulk(cut, "Approve all")!.Click();

        cut.Render(ps => ps.Add(p => p.Ai, AiState.Thinking).Add(p => p.PendingCount, 0));
        Assert.NotEmpty(cut.FindAll(".tool-group-actions button"));   // still mounted, fading
        foreach (var b in cut.FindAll(".tool-group-actions button")) b.Click();

        Assert.Equal(1, decisions);
    }
}
