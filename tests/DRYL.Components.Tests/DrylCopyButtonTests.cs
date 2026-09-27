using Bunit;
using DRYL.Components;
using Microsoft.JSInterop;

namespace DRYL.Components.Tests;

/// <summary>
/// Tests for <see cref="DrylCopyButton"/> — <c>specs/E2 Actions/F4 DrylCopyButton.md</c>.
/// The clipboard itself lives in <c>dryl.clipboard.copy</c>; here the call is planned in
/// strict JSInterop so an unexpected identifier or argument fails the test.
/// </summary>
public class DrylCopyButtonTests : BunitContext
{
    private const string Copy = "dryl.clipboard.copy";

    private IRenderedComponent<DrylCopyButton> RenderButton(
        Action<ComponentParameterCollectionBuilder<DrylCopyButton>>? extra = null)
        => Render<DrylCopyButton>(ps =>
        {
            ps.Add(p => p.Text, "+49 170 1234567");
            extra?.Invoke(ps);
        });

    private static string GlyphState(IRenderedComponent<DrylCopyButton> cut)
    {
        var glyph = cut.Find(".copy-btn-glyph");
        return new[] { "is-idle", "is-copied", "is-failed" }.Single(c => glyph.ClassList.Contains(c));
    }

    [Fact]
    public void Without_a_label_the_button_is_icon_only_named_Copy_and_wrapped_in_a_tooltip()
    {
        var cut = RenderButton();

        var button = cut.Find("button");
        Assert.Equal("Copy", button.GetAttribute("aria-label"));
        Assert.Contains("btn-icon", button.ClassList);
        Assert.Equal("Copy", cut.Find(".tt-wrap").GetAttribute("data-tt"));
        Assert.Empty(cut.FindAll(".copy-btn-text"));
    }

    [Fact]
    public void AriaLabel_names_the_icon_only_button_and_its_tooltip()
    {
        var cut = RenderButton(ps => ps.Add(p => p.AriaLabel, "Nummer kopieren"));

        Assert.Equal("Nummer kopieren", cut.Find("button").GetAttribute("aria-label"));
        Assert.Equal("Nummer kopieren", cut.Find(".tt-wrap").GetAttribute("data-tt"));
    }

    [Fact]
    public void A_label_is_shown_and_stays_the_accessible_name_without_a_tooltip()
    {
        var cut = RenderButton(ps => ps.Add(p => p.Label, "Kopieren"));

        var button = cut.Find("button");
        Assert.Null(button.GetAttribute("aria-label"));
        Assert.DoesNotContain("btn-icon", button.ClassList);
        Assert.Empty(cut.FindAll(".tt-wrap"));
        Assert.Equal("Kopieren", cut.Find(".copy-btn-label--idle").TextContent);
        Assert.Null(cut.Find(".copy-btn-label--idle").GetAttribute("aria-hidden"));
        Assert.Equal("true", cut.Find(".copy-btn-label--copied").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Defaults_are_a_small_ghost_button_at_rest()
    {
        var cut = RenderButton();

        var button = cut.Find("button");
        Assert.Contains("btn-ghost", button.ClassList);
        Assert.Contains("btn-sm", button.ClassList);
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("is-idle", GlyphState(cut));
        Assert.Equal("", cut.Find("[role=status]").TextContent);
    }

    [Fact]
    public void Variant_Size_Class_and_attributes_reach_the_button()
    {
        var cut = RenderButton(ps => ps
            .Add(p => p.Variant, DrylButton.ButtonVariant.Secondary)
            .Add(p => p.Size, DrylButton.ButtonSize.Large)
            .Add(p => p.Class, "extra")
            .AddUnmatched("data-test", "copy"));

        var button = cut.Find("button");
        Assert.Contains("btn-secondary", button.ClassList);
        Assert.Contains("btn-lg", button.ClassList);
        Assert.Contains("extra", button.ClassList);
        Assert.Contains("copy-btn", button.ClassList);
        Assert.Equal("copy", button.GetAttribute("data-test"));
    }

    [Fact]
    public void Pressing_copies_the_text_through_the_clipboard_helper_and_quits_it()
    {
        JSInterop.Setup<bool>(Copy, "+49 170 1234567").SetResult(true);
        var copied = 0;
        var cut = RenderButton(ps => ps.Add(p => p.OnCopied, () => copied++));

        cut.Find("button").Click();

        Assert.Single(JSInterop.Invocations, i => i.Identifier == Copy);
        Assert.Equal("is-copied", GlyphState(cut));
        Assert.Equal("Copied", cut.Find("[role=status]").TextContent);
        Assert.Equal("Copied", cut.Find(".tt-wrap").GetAttribute("data-tt"));
        Assert.Equal(1, copied);
        // The button's own name does not change; the live region carries the news.
        Assert.Equal("Copy", cut.Find("button").GetAttribute("aria-label"));
    }

    [Fact]
    public void CopiedLabel_is_shown_and_announced()
    {
        JSInterop.Setup<bool>(Copy, "+49 170 1234567").SetResult(true);
        var cut = RenderButton(ps => ps
            .Add(p => p.Label, "Kopieren")
            .Add(p => p.CopiedLabel, "Kopiert"));

        cut.Find("button").Click();

        Assert.Equal("Kopiert", cut.Find(".copy-btn-label--copied").TextContent);
        Assert.Contains("is-copied", cut.Find(".copy-btn-text").ClassList);
        Assert.Equal("Kopiert", cut.Find("[role=status]").TextContent);
    }

    [Fact]
    public void A_refused_copy_is_quit_with_FailedLabel_and_raises_no_OnCopied()
    {
        JSInterop.Setup<bool>(Copy, "+49 170 1234567").SetResult(false);
        var copied = 0;
        var cut = RenderButton(ps => ps
            .Add(p => p.FailedLabel, "Kopieren fehlgeschlagen")
            .Add(p => p.OnCopied, () => copied++));

        cut.Find("button").Click();

        Assert.Equal("is-failed", GlyphState(cut));
        Assert.Equal("Kopieren fehlgeschlagen", cut.Find("[role=status]").TextContent);
        Assert.Equal("Kopieren fehlgeschlagen", cut.Find(".tt-wrap").GetAttribute("data-tt"));
        Assert.Equal(0, copied);
    }

    [Fact]
    public void A_throwing_clipboard_helper_is_quit_as_a_failure()
    {
        JSInterop.Setup<bool>(Copy, "+49 170 1234567").SetException(new JSException("denied"));
        var cut = RenderButton();

        cut.Find("button").Click();

        Assert.Equal("is-failed", GlyphState(cut));
        Assert.Equal("Copy failed", cut.Find("[role=status]").TextContent);
    }

    [Fact]
    public void The_confirmation_returns_to_rest_after_the_reset_delay()
    {
        JSInterop.Setup<bool>(Copy, "+49 170 1234567").SetResult(true);
        var cut = RenderButton();

        cut.Find("button").Click();
        Assert.Equal("is-copied", GlyphState(cut));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("is-idle", GlyphState(cut));
            Assert.Equal("", cut.Find("[role=status]").TextContent);
        }, DrylCopyButton.ResetDelay + TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void All_three_glyphs_are_icons_from_the_set()
    {
        var cut = RenderButton();

        var glyphs = cut.FindComponents<DrylIcon>().Select(i => i.Instance.Name).ToArray();
        Assert.Equal(["Copy", "Check", "Alert"], glyphs);
        Assert.All(glyphs, n => Assert.True(DrylIcon.Icons.ContainsKey(n)));
        Assert.Equal("true", cut.Find(".copy-btn-glyph").GetAttribute("aria-hidden"));
    }
}
