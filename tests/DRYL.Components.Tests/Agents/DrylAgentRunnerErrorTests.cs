using System.Runtime.CompilerServices;
using DRYL.Components;
using DRYL.Components.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Tests.Agents;

public class DrylAgentRunnerErrorTests
{
    private static AgentResponseUpdate Content(params AIContent[] c) =>
        new(ChatRole.Assistant, c.ToList());

    private static async IAsyncEnumerable<AgentResponseUpdate> Failing(
        string message, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.Yield();
        yield return Content(new TextContent("partial "));
        throw new InvalidOperationException(message);
    }

    [Fact]
    public async Task Faulted_stream_surfaces_Error_with_message_and_exception_type()
    {
        var runner = new DrylAgentRunner();
        var run = runner.StartFromUpdates(Failing("model exploded"), aiKey: null, ct: default);

        await run.WaitForCompletionAsync();

        Assert.NotNull(run.Error);
        Assert.Equal("model exploded", run.Error!.Message);
        Assert.Equal(nameof(InvalidOperationException), run.Error.ExceptionType);
        Assert.IsType<InvalidOperationException>(run.Error.Exception);
        Assert.Equal(AiState.None, run.State);
        Assert.Equal("partial ", run.Text);   // text before the fault is preserved
    }

    [Fact]
    public async Task Faulted_stream_raises_OnChange_after_Error_is_set()
    {
        var runner = new DrylAgentRunner();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async IAsyncEnumerable<AgentResponseUpdate> Gated(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            await gate.Task;
            yield return Content(new TextContent("x"));
            throw new InvalidOperationException("boom");
        }

        var run = runner.StartFromUpdates(Gated(), aiKey: null, ct: default);
        var errorObservedOnChange = false;
        run.OnChange += () => errorObservedOnChange |= run.Error is not null;
        gate.SetResult();

        await run.WaitForCompletionAsync();

        Assert.True(errorObservedOnChange);
    }

    [Fact]
    public async Task Cancellation_is_not_an_error()
    {
        var runner = new DrylAgentRunner();
        using var cts = new CancellationTokenSource();

        async IAsyncEnumerable<AgentResponseUpdate> Endless(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                yield return Content(new TextContent("."));
                await Task.Delay(10, ct);
            }
        }

        var run = runner.StartFromUpdates(Endless(cts.Token), aiKey: null, ct: cts.Token);
        cts.CancelAfter(30);

        await run.WaitForCompletionAsync();

        Assert.Null(run.Error);
        Assert.Equal(AiState.None, run.State);
    }
}
