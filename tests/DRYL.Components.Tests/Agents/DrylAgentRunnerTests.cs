using System.Runtime.CompilerServices;
using DRYL.Components;
using DRYL.Components.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Tests.Agents;

public class DrylAgentRunnerTests
{
    private static async IAsyncEnumerable<AgentResponseUpdate> Updates(
        IEnumerable<AgentResponseUpdate> items,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var i in items) { ct.ThrowIfCancellationRequested(); await Task.Yield(); yield return i; }
    }

    private static AgentResponseUpdate Content(params AIContent[] c) =>
        new(ChatRole.Assistant, c.ToList());

    [Fact]
    public async Task Text_deltas_accumulate_and_drive_Streaming_then_Generated()
    {
        var runner = new DrylAgentRunner();
        var run = runner.StartFromUpdates(Updates(new[]
        {
            Content(new TextContent("Hel")),
            Content(new TextContent("lo")),
        }), aiKey: null, ct: default);

        await run.WaitForCompletionAsync();

        Assert.Equal("Hello", run.Text);
        Assert.Equal(AiState.Generated, run.State);
    }

    [Fact]
    public async Task Tool_call_then_result_maps_to_invocation_with_Generated_state()
    {
        var runner = new DrylAgentRunner();
        var run = runner.StartFromUpdates(Updates(new AgentResponseUpdate[]
        {
            Content(new FunctionCallContent("call-1", "get_weather",
                new Dictionary<string, object?> { ["city"] = "Berlin" }!)),
            Content(new FunctionResultContent("call-1", "sunny")),
            Content(new TextContent("It is sunny.")),
        }), aiKey: null, ct: default);

        await run.WaitForCompletionAsync();

        var call = Assert.Single(run.ToolCalls);
        Assert.Equal("get_weather", call.ToolName);
        Assert.Contains("Berlin", call.Arguments);
        Assert.Contains("sunny", call.Result);
        Assert.Equal(AiState.Generated, call.State);
    }

    [Fact]
    public void TextStream_returns_a_stable_reference()
    {
        // Regression: a fresh ReadAllAsync() per access makes consumers (DrylAiStream) restart
        // and clear their buffer on every re-render, dropping the streamed answer mid-run.
        var run = new DrylAgentRun();
        Assert.Same(run.TextStream, run.TextStream);
    }

    [Fact]
    public async Task Reasoning_before_text_keeps_state_Thinking()
    {
        var runner = new DrylAgentRunner();
        var states = new List<AiState>();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async IAsyncEnumerable<AgentResponseUpdate> Gated(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            await gate.Task;   // don't emit until the test has subscribed
            yield return Content(new TextReasoningContent("hmm"));
        }

        var run = runner.StartFromUpdates(Gated(), aiKey: null, ct: default);
        run.OnChange += () => states.Add(run.State);
        gate.SetResult();

        await run.WaitForCompletionAsync();

        Assert.Contains(AiState.Thinking, states);
        Assert.DoesNotContain(AiState.Streaming, states);
    }
}
