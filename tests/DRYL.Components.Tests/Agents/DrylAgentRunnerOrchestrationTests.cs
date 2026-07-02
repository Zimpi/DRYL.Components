using System.Runtime.CompilerServices;
using DRYL.Components;
using DRYL.Components.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Tests.Agents;

public class DrylAgentRunnerOrchestrationTests
{
    private static AgentResponseUpdate Content(params AIContent[] c) =>
        new(ChatRole.Assistant, c.ToList());

    private static DrylAgentStep Step(
        string name, Func<string, CancellationToken, IAsyncEnumerable<AgentResponseUpdate>> factory) =>
        new() { Name = name, UpdatesFactory = factory };

    private static async IAsyncEnumerable<AgentResponseUpdate> Says(
        string text, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await Task.Yield();
        yield return Content(new TextContent(text));
    }

    private static async IAsyncEnumerable<AgentResponseUpdate> Fails(
        string message, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.Yield();
        yield return Content(new TextContent("…"));
        throw new InvalidOperationException(message);
    }

    // ── Sequential ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Sequential_hands_each_answer_to_the_next_agent()
    {
        var received = new List<string>();
        var runner = new DrylAgentRunner();

        var run = runner.StartSequential(new[]
        {
            Step("Researcher", (msg, ct) => { received.Add(msg); return Says("research notes", ct); }),
            Step("Writer",     (msg, ct) => { received.Add(msg); return Says("final article", ct); }),
        }, "write about glass", ct: default);

        await run.WaitForCompletionAsync();

        Assert.Equal(new[] { "write about glass", "research notes" }, received);
        Assert.Equal("final article", run.Text);        // the flow carries the final answer
        Assert.Equal(AiState.Generated, run.State);
        Assert.All(run.Steps, s => Assert.Equal(AiState.Generated, s.State));
        Assert.Null(run.ActiveIndex);
        Assert.Null(run.Error);
    }

    [Fact]
    public async Task Sequential_step_failure_stops_the_chain_and_names_the_step()
    {
        var runner = new DrylAgentRunner();

        var run = runner.StartSequential(new[]
        {
            Step("Researcher", (_, ct) => Fails("quota exceeded", ct)),
            Step("Writer",     (_, ct) => Says("never runs", ct)),
        }, "go", ct: default);

        await run.WaitForCompletionAsync();

        Assert.NotNull(run.Error);
        Assert.Equal("quota exceeded", run.Error!.Message);
        Assert.Equal("Researcher", run.Error.Source);
        Assert.Equal(AiState.None, run.State);
        Assert.NotNull(run.Steps[0].Error);
        Assert.False(run.Steps[1].HasStarted);          // the second agent was never started
    }

    [Fact]
    public async Task Sequential_aggregates_step_usage_onto_the_flow()
    {
        static async IAsyncEnumerable<AgentResponseUpdate> WithUsage(
            string text, long input, long output,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.Yield();
            yield return new AgentResponseUpdate(ChatRole.Assistant, new List<AIContent>
            {
                new TextContent(text),
                new UsageContent(new UsageDetails
                {
                    InputTokenCount = input, OutputTokenCount = output,
                    TotalTokenCount = input + output,
                }),
            });
        }

        var runner = new DrylAgentRunner();
        var run = runner.StartSequential(new[]
        {
            Step("A", (_, ct) => WithUsage("a", 100, 10, ct)),
            Step("B", (_, ct) => WithUsage("b", 200, 20, ct)),
        }, "go", ct: default);

        await run.WaitForCompletionAsync();

        Assert.Equal(300, run.Usage!.InputTokens);
        Assert.Equal(30, run.Usage.OutputTokens);
        Assert.Equal(330, run.Usage.TotalTokens);
        Assert.Equal(110, run.Steps[0].Run!.Usage!.TotalTokens);   // per-step usage stays intact
    }

    [Fact]
    public async Task Sequential_flow_streams_the_final_agents_text()
    {
        var runner = new DrylAgentRunner();
        var run = runner.StartSequential(new[]
        {
            Step("A", (_, ct) => Says("intermediate", ct)),
            Step("B", (_, ct) => Says("final", ct)),
        }, "go", ct: default);

        var streamed = new List<string>();
        var reader = Task.Run(async () =>
        {
            await foreach (var delta in run.TextStream) streamed.Add(delta);
        });

        await run.WaitForCompletionAsync();
        await reader;

        Assert.Equal("final", string.Concat(streamed));   // intermediate text never leaks out
    }

    // ── Concurrent ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Concurrent_runs_all_lanes_on_the_same_message()
    {
        var received = new List<string>();
        var runner = new DrylAgentRunner();

        var run = runner.StartConcurrent(new[]
        {
            Step("A", (msg, ct) => { lock (received) received.Add(msg); return Says("alpha", ct); }),
            Step("B", (msg, ct) => { lock (received) received.Add(msg); return Says("beta", ct); }),
        }, "same input", ct: default);

        await run.WaitForCompletionAsync();

        Assert.Equal(new[] { "same input", "same input" }, received);
        Assert.Equal(AiState.Generated, run.State);
        Assert.Equal("alpha", run.Steps[0].Run!.Text);
        Assert.Equal("beta", run.Steps[1].Run!.Text);
        Assert.Equal(string.Empty, run.Text);            // no single final answer in a fan-out
    }

    [Fact]
    public async Task Concurrent_lane_failure_marks_the_flow_but_other_lanes_finish()
    {
        var runner = new DrylAgentRunner();
        var run = runner.StartConcurrent(new[]
        {
            Step("Good", (_, ct) => Says("fine", ct)),
            Step("Bad",  (_, ct) => Fails("lane down", ct)),
        }, "go", ct: default);

        await run.WaitForCompletionAsync();

        Assert.Equal("Bad", run.Error!.Source);
        Assert.Equal("lane down", run.Error.Message);
        Assert.Equal(AiState.None, run.State);
        Assert.Equal(AiState.Generated, run.Steps[0].State);   // the healthy lane completed
    }

    // ── Guards ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Empty_step_list_throws()
    {
        var runner = new DrylAgentRunner();
        Assert.Throws<ArgumentException>(
            () => runner.StartSequential(Array.Empty<DrylAgentStep>(), "go"));
    }
}
