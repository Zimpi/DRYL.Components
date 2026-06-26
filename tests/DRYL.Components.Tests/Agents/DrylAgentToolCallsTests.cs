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
}
