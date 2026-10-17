using System.Threading.Channels;
using Bunit;
using DRYL.Components;
using DRYL.Components.Ai;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests;

/// <summary>
/// <c>Paused</c>: an open stream nobody writes into reads as <see cref="AiState.None"/> instead of
/// <see cref="AiState.Streaming"/> — the case of a run waiting for a tool approval (Trello #68).
/// </summary>
public class DrylAiStreamPauseTests : BunitContext
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

    private AiState _seen;

    public DrylAiStreamPauseTests() => Services.AddDrylComponents();

    private IDrylAiActivityService Activity => Services.GetRequiredService<IDrylAiActivityService>();

    private IRenderedComponent<DrylAiStream> Stream(IAsyncEnumerable<string> source, bool paused) =>
        Render<DrylAiStream>(p => p
            .Add(x => x.Source, source)
            .Add(x => x.Key, "answer")
            .Add(x => x.Paused, paused)
            .Add(x => x.ChildContent, (RenderFragment<AiStreamContext>)(ctx => builder =>
            {
                _seen = ctx.State;
                builder.AddContent(0, ctx.Text);
            })));

    [Fact]
    public void An_open_stream_reads_as_streaming_until_it_is_paused()
    {
        var channel = Channel.CreateUnbounded<string>();
        var cut = Stream(channel.Reader.ReadAllAsync(), paused: false);

        cut.InvokeAsync(() => channel.Writer.TryWrite("Hello"));
        cut.WaitForAssertion(() => Assert.Equal(AiState.Streaming, _seen), Wait);
        Assert.Equal(AiState.Streaming, Activity.GetState("answer"));

        cut.Render(p => p.Add(x => x.Paused, true));

        cut.WaitForAssertion(() => Assert.Equal(AiState.None, _seen), Wait);
        Assert.Equal(AiState.None, Activity.GetState("answer"));
        Assert.Contains("Hello", cut.Markup);   // the text stays
    }

    [Fact]
    public void Unpausing_restores_the_live_state_without_restarting()
    {
        var channel = Channel.CreateUnbounded<string>();
        var cut = Stream(channel.Reader.ReadAllAsync(), paused: true);

        cut.InvokeAsync(() => channel.Writer.TryWrite("Hello "));
        cut.WaitForAssertion(() => Assert.Contains("Hello", cut.Markup), Wait);
        Assert.Equal(AiState.None, _seen);

        // Tokens that still arrive while paused keep accumulating.
        cut.InvokeAsync(() => channel.Writer.TryWrite("world"));
        cut.WaitForAssertion(() => Assert.Contains("Hello world", cut.Markup), Wait);
        Assert.Equal(AiState.None, _seen);

        cut.Render(p => p.Add(x => x.Paused, false));

        cut.WaitForAssertion(() => Assert.Equal(AiState.Streaming, _seen), Wait);
        Assert.Equal(AiState.Streaming, Activity.GetState("answer"));
        Assert.Contains("Hello world", cut.Markup);   // no restart cleared the text
    }

    [Fact]
    public void A_paused_stream_that_has_not_started_does_not_read_as_thinking()
    {
        var channel = Channel.CreateUnbounded<string>();
        var cut = Stream(channel.Reader.ReadAllAsync(), paused: true);

        cut.WaitForAssertion(() => Assert.Equal(AiState.None, _seen), Wait);

        cut.Render(p => p.Add(x => x.Paused, false));
        cut.WaitForAssertion(() => Assert.Equal(AiState.Thinking, _seen), Wait);
    }

    [Fact]
    public void A_stream_that_ends_while_paused_still_reaches_generated()
    {
        var channel = Channel.CreateUnbounded<string>();
        var cut = Stream(channel.Reader.ReadAllAsync(), paused: true);

        cut.InvokeAsync(() =>
        {
            channel.Writer.TryWrite("Done.");
            channel.Writer.TryComplete();
        });

        cut.WaitForAssertion(() => Assert.Equal(AiState.Generated, _seen), Wait);
        Assert.Equal(AiState.Generated, Activity.GetState("answer"));
    }
}
