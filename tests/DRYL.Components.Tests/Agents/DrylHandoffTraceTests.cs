using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;

namespace DRYL.Components.Tests.Agents;

/// <summary>JSInterop is Loose because the nested DrylPresence wires dryl.motion.onExit.</summary>
public class DrylHandoffTraceTests : BunitContext
{
    public DrylHandoffTraceTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private static DrylMultiAgentRun Flow(DrylAgentFlow flow, params string[] names) =>
        new(flow, names);

    [Fact]
    public void Renders_one_lane_per_step_with_pending_badges()
    {
        var run = Flow(DrylAgentFlow.Sequential, "Researcher", "Writer", "Reviewer");
        var cut = Render<DrylHandoffTrace>(p => p.Add(x => x.Run, run));

        Assert.Equal(3, cut.FindAll(".handoff-step").Count);
        Assert.Contains("Researcher", cut.Markup);
        Assert.Contains("Writer", cut.Markup);
        Assert.Contains("Pending", cut.Markup);
    }

    [Fact]
    public void Active_step_wears_the_shared_ai_aura()
    {
        var run = Flow(DrylAgentFlow.Sequential, "A", "B");
        run.Steps[0].Run = new DrylAgentRun { State = AiState.Streaming };

        var cut = Render<DrylHandoffTrace>(p => p.Add(x => x.Run, run));

        var body = cut.Find(".handoff-step--active .handoff-body");
        Assert.Contains("ai-aura", body.ClassList);
        Assert.Contains("ai-streaming", body.ClassList);
        Assert.Single(cut.FindAll(".ai-aura-ring"));
        Assert.Contains("Streaming", cut.Markup);
    }

    [Fact]
    public void Completed_step_fills_the_connector_into_the_next_lane()
    {
        var run = Flow(DrylAgentFlow.Sequential, "A", "B");
        run.Steps[0].Run = new DrylAgentRun { State = AiState.Generated };

        var cut = Render<DrylHandoffTrace>(p => p.Add(x => x.Run, run));

        Assert.Single(cut.FindAll(".handoff-connector--done"));
        Assert.Contains("Done", cut.Markup);
    }

    [Fact]
    public void Failed_step_shows_danger_badge_and_error_alert()
    {
        var run = Flow(DrylAgentFlow.Sequential, "A", "B");
        run.Steps[0].Run = new DrylAgentRun
        {
            State = AiState.None,
            Error = new DrylRunError("quota exceeded"),
        };

        var cut = Render<DrylHandoffTrace>(p => p.Add(x => x.Run, run));

        Assert.Contains("Failed", cut.Markup);
        Assert.Contains("quota exceeded", cut.Markup);
        Assert.Single(cut.FindAll(".handoff-node--failed"));
    }

    [Fact]
    public void Concurrent_flow_renders_no_visible_connectors()
    {
        var sequential = Render<DrylHandoffTrace>(p =>
            p.Add(x => x.Run, Flow(DrylAgentFlow.Sequential, "A", "B")));
        Assert.Single(sequential.FindAll(".handoff-connector"));

        var concurrent = Render<DrylHandoffTrace>(p =>
            p.Add(x => x.Run, Flow(DrylAgentFlow.Concurrent, "A", "B")));
        Assert.Contains("handoff-trace--concurrent", concurrent.Find(".handoff-trace").ClassList);
        Assert.Empty(concurrent.FindAll(".handoff-connector"));
    }

    [Fact]
    public void StepContent_renders_per_lane_with_the_handoff_as_context()
    {
        var run = Flow(DrylAgentFlow.Sequential, "A", "B");
        var cut = Render<DrylHandoffTrace>(p => p
            .Add(x => x.Run, run)
            .Add(x => x.StepContent, step => $"<em>lane-{step.Index}</em>"));

        Assert.Contains("lane-0", cut.Markup);
        Assert.Contains("lane-1", cut.Markup);
    }

    [Fact]
    public void Updates_live_when_the_run_raises_OnChange()
    {
        var run = Flow(DrylAgentFlow.Sequential, "A", "B");
        var cut = Render<DrylHandoffTrace>(p => p.Add(x => x.Run, run));
        Assert.Contains("Pending", cut.Markup);

        run.Steps[0].Run = new DrylAgentRun { State = AiState.Thinking };
        run.Raise();

        cut.WaitForAssertion(() => Assert.Contains("Running", cut.Markup));
    }

    [Fact]
    public void A_lane_that_completes_plays_the_generated_one_shot()
    {
        var run = Flow(DrylAgentFlow.Sequential, "A", "B");
        run.Steps[0].Run = new DrylAgentRun { State = AiState.Streaming };
        var cut = Render<DrylHandoffTrace>(p => p.Add(x => x.Run, run));

        run.Steps[0].Run!.State = AiState.Generated;
        run.Raise();

        cut.WaitForAssertion(() =>
        {
            var body = cut.Find(".handoff-step--done .handoff-body");
            Assert.Contains("ai-generated", body.ClassList);
            Assert.Single(cut.FindAll(".ai-aura-wash"));
        });
    }

    [Fact]
    public void A_lane_already_done_when_the_trace_mounts_is_shown_settled()
    {
        var run = Flow(DrylAgentFlow.Sequential, "A", "B");
        run.Steps[0].Run = new DrylAgentRun { State = AiState.Generated };

        var cut = Render<DrylHandoffTrace>(p => p.Add(x => x.Run, run));

        Assert.DoesNotContain("ai-aura", cut.Find(".handoff-step--done .handoff-body").ClassList);
        Assert.Empty(cut.FindAll(".ai-aura-ring, .ai-aura-wash"));
    }

    [Fact]
    public void A_lane_that_fails_fades_its_aura_out_instead_of_cutting_it()
    {
        var run = Flow(DrylAgentFlow.Sequential, "A", "B");
        run.Steps[0].Run = new DrylAgentRun { State = AiState.Thinking };
        var cut = Render<DrylHandoffTrace>(p => p.Add(x => x.Run, run));

        run.Steps[0].Run!.Error = new DrylRunError("boom");
        run.Steps[0].Run!.State = AiState.None;
        run.Raise();

        cut.WaitForAssertion(() =>
        {
            var body = cut.Find(".handoff-step--failed .handoff-body");
            Assert.Contains("ai-aura--out", body.ClassList);
            Assert.Single(cut.FindAll(".ai-aura-ring"));
        });
        cut.WaitForAssertion(
            () => Assert.Empty(cut.FindAll(".ai-aura-ring")),
            TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Each_lane_of_a_concurrent_run_carries_its_own_aura()
    {
        var run = Flow(DrylAgentFlow.Concurrent, "A", "B");
        run.Steps[0].Run = new DrylAgentRun { State = AiState.Thinking };
        run.Steps[1].Run = new DrylAgentRun { State = AiState.Streaming };

        var cut = Render<DrylHandoffTrace>(p => p.Add(x => x.Run, run));

        Assert.Equal(2, cut.FindAll(".ai-aura-ring").Count);
        Assert.Single(cut.FindAll(".handoff-body.ai-thinking"));
        Assert.Single(cut.FindAll(".handoff-body.ai-streaming"));
    }
}
