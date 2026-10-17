using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;

namespace DRYL.Components.Tests.Agents;

public class DrylAgentToolCallsTests : BunitContext
{
    [Fact]
    public void Renders_one_tool_call_per_invocation()
    {
        var run = new DrylAgentRun();
        run.AddToolCall(new DrylToolInvocation { CallId = "1", ToolName = "alpha", Result = "\"ok\"" });
        run.AddToolCall(new DrylToolInvocation { CallId = "2", ToolName = "beta" });

        var cut = Render<DrylAgentToolCalls>(p => p.Add(x => x.Run, run));

        Assert.Equal(2, cut.FindAll(".tool-call").Count);
        Assert.Contains("alpha", cut.Markup);
        Assert.Contains("beta", cut.Markup);
    }

    [Fact]
    public void ActiveOnly_shows_only_running_calls()
    {
        var run = new DrylAgentRun();
        run.AddToolCall(new DrylToolInvocation { CallId = "1", ToolName = "done", Result = "\"ok\"" });
        run.AddToolCall(new DrylToolInvocation { CallId = "2", ToolName = "running" });

        var cut = Render<DrylAgentToolCalls>(p => p
            .Add(x => x.Run, run)
            .Add(x => x.ActiveOnly, true));

        Assert.Single(cut.FindAll(".tool-call"));
        Assert.Contains("running", cut.Markup);
        Assert.DoesNotContain("done", cut.Markup);
    }

    // ── Approval (I21) ────────────────────────────────────────────────

    private static DrylAgentRun PendingRun(params string[] ids)
    {
        var run = new DrylAgentRun();
        foreach (var id in ids)
            run.AddToolCall(new DrylToolInvocation { CallId = id, ToolName = $"tool_{id}", Approval = DrylToolApproval.Pending });
        return run;
    }

    private IRenderedComponent<DrylAgentToolCalls> RenderRun(DrylRunBase run, bool activeOnly = false)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        return Render<DrylAgentToolCalls>(p => p.Add(x => x.Run, run).Add(x => x.ActiveOnly, activeOnly));
    }

    [Fact]
    public void Pending_calls_put_the_group_at_proposed_with_a_pending_count()
    {
        var run = PendingRun("1", "2");
        run.AddToolCall(new DrylToolInvocation { CallId = "3", ToolName = "done", Result = "\"ok\"" });

        var cut = RenderRun(run);

        var group = cut.FindComponent<DrylToolCallGroup>().Instance;
        Assert.Equal(AiState.Proposed, group.Ai);
        Assert.Equal(2, group.PendingCount);
    }

    [Fact]
    public void A_running_call_outranks_pending_ones_for_the_group_state()
    {
        var run = PendingRun("1");
        run.AddToolCall(new DrylToolInvocation { CallId = "2", ToolName = "running" });

        var cut = RenderRun(run);

        Assert.Equal(AiState.Thinking, cut.FindComponent<DrylToolCallGroup>().Instance.Ai);
    }

    [Fact]
    public void Settled_calls_leave_the_group_at_none()
    {
        var run = new DrylAgentRun();
        run.AddToolCall(new DrylToolInvocation { CallId = "1", ToolName = "a", Result = "\"ok\"" });

        var cut = RenderRun(run);

        Assert.Equal(AiState.None, cut.FindComponent<DrylToolCallGroup>().Instance.Ai);
    }

    [Fact]
    public void ActiveOnly_includes_pending_calls()
    {
        var run = PendingRun("1");
        run.AddToolCall(new DrylToolInvocation { CallId = "2", ToolName = "done", Result = "\"ok\"" });

        var cut = RenderRun(run, activeOnly: true);

        Assert.Single(cut.FindAll(".tool-call"));
        Assert.Contains("tool_1", cut.Markup);
    }

    [Fact]
    public void A_card_approve_approves_that_call()
    {
        var run = PendingRun("1", "2");
        var cut = RenderRun(run);

        cut.FindAll(".tool-call-actions button").First(b => b.GetAttribute("aria-label") == "Approve tool_2").Click();

        Assert.Equal(DrylToolApproval.Pending, run.ToolCalls[0].Approval);
        Assert.Equal(DrylToolApproval.Approved, run.ToolCalls[1].Approval);
    }

    [Fact]
    public void A_card_deny_declines_that_call_and_marks_the_card()
    {
        var run = PendingRun("1");
        var cut = RenderRun(run);

        cut.FindAll(".tool-call-actions button").First(b => b.GetAttribute("aria-label") == "Deny tool_1").Click();

        Assert.Equal(DrylToolApproval.Declined, run.ToolCalls[0].Approval);
        Assert.True(cut.FindComponent<DrylToolCall>().Instance.Declined);
        Assert.Contains("Declined", cut.Find(".tool-call-status").TextContent);
    }

    [Fact]
    public void Approve_all_approves_every_pending_call()
    {
        var run = PendingRun("1", "2", "3");
        var cut = RenderRun(run);

        cut.FindAll(".tool-group-actions button").First(b => b.TextContent.Trim() == "Approve all").Click();

        Assert.All(run.ToolCalls, t => Assert.Equal(DrylToolApproval.Approved, t.Approval));
        Assert.Empty(run.PendingApprovals);
    }

    [Fact]
    public void Deny_all_denies_every_pending_call()
    {
        var run = PendingRun("1", "2");
        var cut = RenderRun(run);

        cut.FindAll(".tool-group-actions button").First(b => b.TextContent.Trim() == "Deny all").Click();

        Assert.All(run.ToolCalls, t => Assert.Equal(DrylToolApproval.Declined, t.Approval));
    }

    [Fact]
    public void Labels_reach_the_cards_and_the_group()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<DrylAgentToolCalls>(p => p
            .Add(x => x.Run, PendingRun("1", "2"))
            .Add(x => x.ApproveLabel, "Freigeben").Add(x => x.DenyLabel, "Ablehnen")
            .Add(x => x.ApproveAllLabel, "Alle freigeben").Add(x => x.DenyAllLabel, "Alle ablehnen"));

        var card = cut.FindComponents<DrylToolCall>()[0].Instance;
        Assert.Equal("Freigeben", card.ApproveLabel);
        Assert.Equal("Ablehnen", card.DenyLabel);
        var group = cut.FindComponent<DrylToolCallGroup>().Instance;
        Assert.Equal("Alle freigeben", group.ApproveAllLabel);
        Assert.Equal("Alle ablehnen", group.DenyAllLabel);
    }

    [Fact]
    public void Unsubscribes_when_the_run_changes()
    {
        var first = PendingRun("1");
        var cut = RenderRun(first);
        var renders = cut.RenderCount;

        cut.Render(p => p.Add(x => x.Run, new DrylAgentRun()));
        var afterSwap = cut.RenderCount;
        first.Approve("1");   // raises OnChange on the old run

        Assert.True(afterSwap > renders);
        Assert.Equal(afterSwap, cut.RenderCount);
    }
}
