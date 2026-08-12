using System.Collections.Concurrent;
using System.Threading.Channels;
using Bunit;
using Microsoft.AspNetCore.Components;
using DRYL.Components;

namespace DRYL.Components.Tests;

public class DrylAiStreamTests : BunitContext
{
    private static (Channel<string> Channel, IAsyncEnumerable<string> Source) MakeSource()
    {
        var channel = Channel.CreateUnbounded<string>();
        return (channel, channel.Reader.ReadAllAsync());
    }

    [Fact]
    public void Direct_mode_renders_each_chunk_as_it_arrives()
    {
        var (channel, source) = MakeSource();
        var cut = Render<DrylAiStream>(p => p.Add(x => x.Source, source));

        // Explicit timeouts: bUnit's default is one second, which a loaded machine can
        // miss without anything being wrong with the component. The waits below are
        // upper bounds, not measurements — the assertions still pass on the first
        // render that satisfies them.
        cut.InvokeAsync(() => channel.Writer.TryWrite("Hello "));
        cut.WaitForAssertion(() => Assert.Contains("Hello", cut.Markup),
            timeout: TimeSpan.FromSeconds(10));

        cut.InvokeAsync(() => channel.Writer.TryWrite("world"));
        channel.Writer.TryComplete();
        cut.WaitForAssertion(() => Assert.Contains("Hello world", cut.Markup),
            timeout: TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Smooth_mode_reveals_a_burst_gradually_and_completely()
    {
        var (channel, source) = MakeSource();
        var burst = string.Concat(Enumerable.Repeat("0123456789", 60)); // 600 chars in one chunk

        // Record how much text each intermediate render carried, as it happens.
        // Sampling the markup after a WaitForAssertion returns cannot prove pacing:
        // the reveal may finish in the gap between the wait returning and the sample
        // being taken, and then the sample reads 600. That race is what made this
        // test flaky under load. A recorded value cannot expire.
        var revealed = new ConcurrentQueue<int>();

        var cut = Render<DrylAiStream>(p => p
            .Add(x => x.Source, source)
            .Add(x => x.Smooth, true)
            .Add(x => x.ChildContent, (RenderFragment<AiStreamContext>)(ctx => builder =>
            {
                revealed.Enqueue(ctx.Text.Length);
                builder.AddContent(0, ctx.Text);
            })));

        cut.InvokeAsync(() =>
        {
            channel.Writer.TryWrite(burst);
            channel.Writer.TryComplete();
        });

        // The whole burst arrives …
        cut.WaitForAssertion(
            () => Assert.Equal(600, cut.Markup.Count(char.IsDigit)),
            timeout: TimeSpan.FromSeconds(10));

        // … and it got there in steps rather than in one dump. Asserted against the
        // recorded history, so no timing window has to be hit.
        Assert.Contains(revealed, n => n > 0 && n < 600);
    }

    [Fact]
    public void Smooth_mode_does_not_change_the_final_text()
    {
        var (channel, source) = MakeSource();
        var cut = Render<DrylAiStream>(p => p
            .Add(x => x.Source, source)
            .Add(x => x.Smooth, true));

        cut.InvokeAsync(() =>
        {
            channel.Writer.TryWrite("Alpha ");
            channel.Writer.TryWrite("Beta ");
            channel.Writer.TryWrite("Gamma");
            channel.Writer.TryComplete();
        });

        cut.WaitForAssertion(
            () => Assert.Contains("Alpha Beta Gamma", cut.Markup),
            timeout: TimeSpan.FromSeconds(10));
    }
}
