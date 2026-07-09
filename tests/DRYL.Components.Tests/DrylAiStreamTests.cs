using System.Threading.Channels;
using Bunit;
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

        cut.InvokeAsync(() => channel.Writer.TryWrite("Hello "));
        cut.WaitForAssertion(() => Assert.Contains("Hello", cut.Markup));

        cut.InvokeAsync(() => channel.Writer.TryWrite("world"));
        channel.Writer.TryComplete();
        cut.WaitForAssertion(() => Assert.Contains("Hello world", cut.Markup));
    }

    [Fact]
    public void Smooth_mode_reveals_a_burst_gradually_and_completely()
    {
        var (channel, source) = MakeSource();
        var burst = string.Concat(Enumerable.Repeat("0123456789", 60)); // 600 chars in one chunk

        var cut = Render<DrylAiStream>(p => p
            .Add(x => x.Source, source)
            .Add(x => x.Smooth, true));

        cut.InvokeAsync(() =>
        {
            channel.Writer.TryWrite(burst);
            channel.Writer.TryComplete();
        });

        // Shortly after the burst arrives, only part of it is revealed (paced, not dumped) …
        cut.WaitForAssertion(() =>
        {
            var text = cut.Markup;
            Assert.Contains("0123456789", text);
        }, timeout: TimeSpan.FromSeconds(5));
        var midway = cut.Markup.Count(c => char.IsDigit(c));
        Assert.True(midway < 600, $"expected a partial reveal, but {midway} chars were already visible");

        // … and eventually the full text is there.
        cut.WaitForAssertion(
            () => Assert.Equal(600, cut.Markup.Count(char.IsDigit)),
            timeout: TimeSpan.FromSeconds(10));
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
