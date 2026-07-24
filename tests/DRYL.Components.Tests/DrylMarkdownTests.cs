using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Rendering tests for <see cref="DrylMarkdown"/>, focused on streaming
/// robustness: the component re-parses the accumulated text on every chunk, so
/// it must tolerate the partial, mid-character states a live token stream emits.
/// </summary>
public class DrylMarkdownTests : BunitContext
{
    [Fact]
    public void Renders_basic_markdown()
    {
        var cut = Render<DrylMarkdown>(ps => ps.Add(p => p.Content, "# Hi\n\nHello **world**"));

        Assert.Contains("Hello", cut.Markup);
        Assert.Contains("<strong>world</strong>", cut.Markup);
    }

    [Fact]
    public void Lone_high_surrogate_in_heading_does_not_throw()
    {
        // A chunk boundary split an emoji (U+1F4C8 📈) surrogate pair, so only the
        // high surrogate has arrived at the tail of a heading line. Markdig's
        // AutoIdentifier would ICU-normalize this and throw
        // "String contains invalid Unicode code points" — the component must
        // sanitize it away instead of tearing down the circuit.
        var ex = Record.Exception(() =>
            Render<DrylMarkdown>(ps => ps.Add(p => p.Content, "# Weekly trend \uD83D")));

        Assert.Null(ex);
    }

    [Fact]
    public void Lone_low_surrogate_does_not_throw()
    {
        var ex = Record.Exception(() =>
            Render<DrylMarkdown>(ps => ps.Add(p => p.Content, "Body text \uDCC8 more")));

        Assert.Null(ex);
    }

    [Fact]
    public void Completed_surrogate_pair_renders()
    {
        // Once the low surrogate arrives, the full emoji is preserved (not stripped).
        var cut = Render<DrylMarkdown>(ps => ps.Add(p => p.Content, "Chart 📈 up"));

        Assert.Contains("📈", cut.Markup);
    }

    [Fact]
    public void Streaming_growth_through_a_split_emoji_never_throws()
    {
        // Walk the accumulated text one char at a time, exactly as a stream grows,
        // re-rendering at every prefix — including the frame with a lone surrogate.
        const string full = "# Sales 📈\n\nUp **12%** week-over-week.";
        var cut = Render<DrylMarkdown>(ps => ps.Add(p => p.Content, ""));

        var ex = Record.Exception(() =>
        {
            for (var i = 1; i <= full.Length; i++)
            {
                var prefix = full.Substring(0, i);
                cut.Render(ps => ps.Add(p => p.Content, prefix));
            }
        });

        Assert.Null(ex);
        Assert.Contains("12%", cut.Markup);
    }

    [Fact]
    public void Unchanged_content_is_not_reparsed()
    {
        var cut = Render<DrylMarkdown>(ps => ps.Add(p => p.Content, "# Hi"));
        var count = cut.Instance.ParseCount;

        cut.Render(ps => ps.Add(p => p.Content, "# Hi"));
        Assert.Equal(count, cut.Instance.ParseCount);

        cut.Render(ps => ps.Add(p => p.Content, "# Hi there"));
        Assert.Equal(count + 1, cut.Instance.ParseCount);
    }
}
