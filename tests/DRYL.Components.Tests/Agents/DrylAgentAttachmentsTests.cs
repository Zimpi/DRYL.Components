using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;

namespace DRYL.Components.Tests.Agents;

public class DrylAgentAttachmentsTests : BunitContext
{
    private static DrylToolInvocation Call(string id, string tool, string args) =>
        new() { CallId = id, ToolName = tool, Arguments = args, Result = "\"ok\"" };

    [Fact]
    public void Renders_stats_call_as_stat_cards()
    {
        var run = new DrylAgentRun();
        run.AddToolCall(Call("1", "show_stats",
            """{"stats":[{"label":"Revenue","value":"10k","delta":"+5%","direction":"up"},{"label":"Churn","value":"2%"}]}"""));

        var cut = Render<DrylAgentAttachments>(p => p.Add(x => x.Run, run));

        Assert.Single(cut.FindAll(".agent-attachments"));
        Assert.Equal(2, cut.FindAll(".stat").Count);
        Assert.Contains("Revenue", cut.Markup);
    }

    [Fact]
    public void Renders_line_chart_with_title()
    {
        var run = new DrylAgentRun();
        run.AddToolCall(Call("1", "show_line_chart",
            """{"title":"Q2","labels":["Apr","May"],"series":[{"name":"Rev","data":[1,2]}]}"""));

        var cut = Render<DrylAgentAttachments>(p => p.Add(x => x.Run, run));

        Assert.Single(cut.FindAll(".chart-kind-line"));
        Assert.Contains("Q2", cut.Markup);
        Assert.Single(cut.FindAll(".agent-attachment-title"));
    }

    [Fact]
    public void Invalid_arguments_render_nothing()
    {
        var run = new DrylAgentRun();
        run.AddToolCall(Call("1", "show_line_chart",
            """{"labels":["Apr","May","Jun"],"series":[{"name":"Rev","data":[1,2]}]}"""));

        var cut = Render<DrylAgentAttachments>(p => p.Add(x => x.Run, run));

        Assert.Empty(cut.FindAll(".agent-attachments"));
    }

    [Fact]
    public void Non_display_tools_are_ignored()
    {
        var run = new DrylAgentRun();
        run.AddToolCall(Call("1", "search_docs", """{"query":"x"}"""));

        var cut = Render<DrylAgentAttachments>(p => p.Add(x => x.Run, run));

        Assert.Empty(cut.FindAll(".agent-attachments"));
    }

    [Fact]
    public void New_tool_calls_appear_on_run_change()
    {
        var run = new DrylAgentRun();
        var cut = Render<DrylAgentAttachments>(p => p.Add(x => x.Run, run));
        Assert.Empty(cut.FindAll(".agent-attachments"));

        cut.InvokeAsync(() => run.AddToolCall(Call("1", "show_donut_chart",
            """{"segments":[{"label":"EU","value":9},{"label":"US","value":6}]}""")));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".chart-kind-donut")));
    }

    [Fact]
    public void Burst_of_calls_reveals_staggered_first_immediately_rest_over_time()
    {
        var run = new DrylAgentRun();
        run.AddToolCall(Call("1", "show_stats", """{"stats":[{"label":"A","value":"1"}]}"""));
        run.AddToolCall(Call("2", "show_stats", """{"stats":[{"label":"B","value":"2"}]}"""));
        run.AddToolCall(Call("3", "show_stats", """{"stats":[{"label":"C","value":"3"}]}"""));

        var cut = Render<DrylAgentAttachments>(p => p.Add(x => x.Run, run));

        // The first attachment of the burst shows immediately, not delayed.
        Assert.Single(cut.FindAll(".agent-attachment"));

        // The rest cascade in via the reveal queue.
        cut.WaitForAssertion(
            () => Assert.Equal(3, cut.FindAll(".agent-attachment").Count),
            timeout: TimeSpan.FromSeconds(5));
    }
}
