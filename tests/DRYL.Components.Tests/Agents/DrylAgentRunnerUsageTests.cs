using System.Runtime.CompilerServices;
using DRYL.Components.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Tests.Agents;

public class DrylAgentRunnerUsageTests
{
    private static AgentResponseUpdate Content(params AIContent[] c) =>
        new(ChatRole.Assistant, c.ToList());

    private static async IAsyncEnumerable<AgentResponseUpdate> Updates(
        IEnumerable<AgentResponseUpdate> items,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var i in items) { ct.ThrowIfCancellationRequested(); await Task.Yield(); yield return i; }
    }

    [Fact]
    public async Task Usage_updates_accumulate_across_the_stream()
    {
        var runner = new DrylAgentRunner();
        var run = runner.StartFromUpdates(Updates(new[]
        {
            Content(new TextContent("Hi")),
            Content(new UsageContent(new UsageDetails
            {
                InputTokenCount = 100, OutputTokenCount = 20, TotalTokenCount = 120,
            })),
            Content(new TextContent(" there")),
            Content(new UsageContent(new UsageDetails
            {
                InputTokenCount = 50, OutputTokenCount = 10, TotalTokenCount = 60,
            })),
        }), aiKey: null, ct: default);

        await run.WaitForCompletionAsync();

        Assert.NotNull(run.Usage);
        Assert.Equal(150, run.Usage!.InputTokens);
        Assert.Equal(30, run.Usage.OutputTokens);
        Assert.Equal(180, run.Usage.TotalTokens);
    }

    [Fact]
    public async Task Missing_counts_stay_null_and_partial_counts_still_sum()
    {
        var runner = new DrylAgentRunner();
        var run = runner.StartFromUpdates(Updates(new[]
        {
            Content(new UsageContent(new UsageDetails { OutputTokenCount = 7 })),
        }), aiKey: null, ct: default);

        await run.WaitForCompletionAsync();

        Assert.NotNull(run.Usage);
        Assert.Null(run.Usage!.InputTokens);
        Assert.Equal(7, run.Usage.OutputTokens);
        Assert.Null(run.Usage.TotalTokens);
    }

    [Fact]
    public async Task No_usage_updates_leaves_Usage_null()
    {
        var runner = new DrylAgentRunner();
        var run = runner.StartFromUpdates(Updates(new[]
        {
            Content(new TextContent("Hello")),
        }), aiKey: null, ct: default);

        await run.WaitForCompletionAsync();

        Assert.Null(run.Usage);
    }

    [Fact]
    public async Task Usage_update_raises_OnChange()
    {
        var runner = new DrylAgentRunner();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async IAsyncEnumerable<AgentResponseUpdate> Gated(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            await gate.Task;
            yield return Content(new UsageContent(new UsageDetails { TotalTokenCount = 5 }));
        }

        var run = runner.StartFromUpdates(Gated(), aiKey: null, ct: default);
        var usageObserved = false;
        run.OnChange += () => usageObserved |= run.Usage is not null;
        gate.SetResult();

        await run.WaitForCompletionAsync();

        Assert.True(usageObserved);
    }
}
