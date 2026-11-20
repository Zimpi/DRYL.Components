using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// <see cref="DrylMarkdown"/> renders model-authored Markdown, which is untrusted:
/// a model can be steered by anything it read. These tests pin the three closed
/// doors — no raw HTML, no attribute blocks, no unsafe link or image targets.
/// </summary>
public class DrylMarkdownSafetyTests : BunitContext
{
    private IRenderedComponent<DrylMarkdown> RenderMd(string content) =>
        Render<DrylMarkdown>(ps => ps.Add(p => p.Content, content));

    [Theory]
    [InlineData("https://dryl.dev/x")]
    [InlineData("http://dryl.dev/x")]
    [InlineData("mailto:hi@dryl.dev")]
    [InlineData("tel:+4912345")]
    public void Allowed_scheme_renders_a_link(string target)
    {
        var cut = RenderMd($"[go]({target})");

        var a = Assert.Single(cut.FindAll("a"));
        Assert.Equal(target, a.GetAttribute("href"));
        Assert.Equal("go", a.TextContent);
    }

    [Theory]
    [InlineData("/docs/start")]
    [InlineData("docs/start")]
    [InlineData("#install")]
    [InlineData("//cdn.dryl.dev/x")]
    public void Schemeless_target_renders_a_link(string target)
    {
        var cut = RenderMd($"[go]({target})");

        Assert.Equal(target, Assert.Single(cut.FindAll("a")).GetAttribute("href"));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("JaVaScRiPt:alert(1)")]
    [InlineData("&#106;avascript:alert(1)")]
    [InlineData("java&#9;script:alert(1)")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==")]
    [InlineData("file:///etc/passwd")]
    public void Disallowed_scheme_keeps_the_text_without_a_link(string target)
    {
        var cut = RenderMd($"Before [click me]({target}) after");

        Assert.Empty(cut.FindAll("a"));
        Assert.Contains("click me", cut.Find(".md-content").TextContent);
    }

    [Fact]
    public void Reference_link_is_checked_against_its_definition()
    {
        var cut = RenderMd("See [the docs][d].\n\n[d]: javascript:alert(1)");

        Assert.Empty(cut.FindAll("a"));
        Assert.Contains("the docs", cut.Markup);
    }

    [Fact]
    public void Reference_link_with_an_allowed_target_still_links()
    {
        var cut = RenderMd("See [the docs][d].\n\n[d]: https://dryl.dev");

        Assert.Equal("https://dryl.dev", Assert.Single(cut.FindAll("a")).GetAttribute("href"));
    }

    [Fact]
    public void Autolink_with_a_disallowed_scheme_renders_as_text()
    {
        var cut = RenderMd("Go <javascript:alert(1)> now");

        Assert.Empty(cut.FindAll("a"));
        Assert.Contains("javascript:alert(1)", cut.Find(".md-content").TextContent);
    }

    [Fact]
    public void Autolinks_with_allowed_targets_still_link()
    {
        var cut = RenderMd("<https://dryl.dev> and <hi@dryl.dev> and www.dryl.dev");

        var hrefs = cut.FindAll("a").Select(a => a.GetAttribute("href")).ToList();
        Assert.Contains("https://dryl.dev", hrefs);
        Assert.Contains("mailto:hi@dryl.dev", hrefs);
        Assert.Contains("http://www.dryl.dev", hrefs);
    }

    [Theory]
    [InlineData("https://dryl.dev/a.png")]
    [InlineData("/img/a.png")]
    public void Allowed_image_source_renders_an_img(string src)
    {
        var cut = RenderMd($"![chart]({src})");

        var img = Assert.Single(cut.FindAll("img"));
        Assert.Equal(src, img.GetAttribute("src"));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/svg+xml;base64,PHN2Zz48L3N2Zz4=")]
    [InlineData("mailto:hi@dryl.dev")]
    public void Disallowed_image_source_renders_the_alt_text(string src)
    {
        var cut = RenderMd($"![the chart]({src})");

        Assert.Empty(cut.FindAll("img"));
        Assert.Contains("the chart", cut.Find(".md-content").TextContent);
    }

    [Theory]
    [InlineData("[a](https://dryl.dev){onclick=\"alert(1)\"}")]
    [InlineData("# Heading {onmouseover=\"alert(1)\"}")]
    [InlineData("A paragraph {style=\"position:fixed;inset:0\"}")]
    [InlineData("[b](https://dryl.dev){href=\"javascript:alert(1)\"}")]
    public void Attribute_block_adds_no_attribute(string content)
    {
        var cut = RenderMd(content);

        foreach (var el in cut.Find(".md-content").QuerySelectorAll("*"))
        {
            Assert.DoesNotContain(el.Attributes, a => a.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase));
            Assert.Null(el.GetAttribute("style"));
            if (el.LocalName == "a") Assert.Equal("https://dryl.dev", el.GetAttribute("href"));
        }
        Assert.Contains("{", cut.Find(".md-content").TextContent);
    }

    [Fact]
    public void Class_attribute_block_adds_no_class()
    {
        var cut = RenderMd("Text {.ai-aura}");

        Assert.Null(cut.Find(".md-content p").GetAttribute("class"));
        Assert.Contains("{.ai-aura}", cut.Find(".md-content").TextContent);
    }
}
