using Bunit;
using DRYL.Components;
using Microsoft.AspNetCore.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// I46 — a consumer style adds to the component's inline style instead of
/// replacing it. One contract, checked on every component that writes an
/// inline style on its root.
/// </summary>
public class ConsumerStyleMergeTests : BunitContext
{
    public ConsumerStyleMergeTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private const string Extra = "margin-top: 8px";

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("display: flex;", null, "display: flex")]
    [InlineData(null, " color: red; ", "color: red")]
    [InlineData("display: flex; gap: 0;", "display: block;", "display: flex; gap: 0; display: block")]
    [InlineData("  ", "", null)]
    public void Merge_writes_own_first_and_consumer_last(string? own, string? consumer, string? expected) =>
        Assert.Equal(expected, InlineStyle.Merge(own, consumer));

    /// <summary>Renders <typeparamref name="T"/> with a lowercase <c>style</c> attribute, as a consumer writes it.</summary>
    private string RootStyle<T>(string selector, Action<ComponentParameterCollectionBuilder<T>>? configure = null)
        where T : IComponent
    {
        var cut = Render<T>(p =>
        {
            configure?.Invoke(p);
            p.AddUnmatched("style", Extra);
        });
        return cut.Find(selector).GetAttribute("style") ?? string.Empty;
    }

    private static void AssertMerged(string style, string own)
    {
        Assert.Contains(own, style);
        Assert.EndsWith(Extra, style);
        Assert.True(style.IndexOf(own, StringComparison.Ordinal) < style.IndexOf(Extra, StringComparison.Ordinal));
    }

    [Fact] public void Stack_keeps_its_flex_layout() =>
        AssertMerged(RootStyle<DrylStack>(".stack"), "display: flex");

    [Fact] public void Grid_keeps_its_template() =>
        AssertMerged(RootStyle<DrylGrid>("[style]"), "gap:");

    [Fact] public void Container_keeps_its_width() =>
        AssertMerged(RootStyle<DrylContainer>(".container"), "max-width:");

    [Fact] public void AspectRatio_keeps_its_ratio() =>
        AssertMerged(RootStyle<DrylAspectRatio>(".aspect"), "aspect-ratio:");

    [Fact] public void Spacer_keeps_its_flex() =>
        AssertMerged(RootStyle<DrylSpacer>(".spacer"), "flex: 1 1 auto");

    [Fact] public void ScrollArea_keeps_its_max_height() =>
        AssertMerged(RootStyle<DrylScrollArea>("[style]", p => p.Add(x => x.MaxHeight, "12rem")), "max-height: 12rem");

    [Fact] public void Typo_keeps_colour_and_alignment() =>
        AssertMerged(RootStyle<DrylTypo>(".typo", p => p.Add(x => x.Align, DrylTypo.TypoAlign.Center)), "text-align: center");

    [Fact] public void Drawer_keeps_its_width_variable() =>
        AssertMerged(RootStyle<DrylDrawer>(".sidebar", p => p.Add(x => x.Width, "20rem")), "--sidebar-w:20rem");

    [Fact] public void Layout_keeps_its_width_variable() =>
        AssertMerged(RootStyle<DrylLayout>(".app-shell", p => p.Add(x => x.SidebarWidth, "20rem")), "--sidebar-w:20rem");

    [Fact] public void Image_keeps_its_wrapper_style() =>
        AssertMerged(RootStyle<DrylImage>("[style]", p => p
            .Add(x => x.Src, "a.png").Add(x => x.Alt, "a").Add(x => x.Ratio, DrylImage.ImageRatio.Square)), "aspect-ratio:");

    [Fact] public void Calendar_keeps_its_height() =>
        AssertMerged(RootStyle<DrylCalendar>(".cal, [class^=cal]", p => p.Add(x => x.Height, "30rem")), "height: 30rem");

    [Fact]
    public void A_component_with_nothing_of_its_own_writes_the_consumer_style_alone()
    {
        Assert.Equal(Extra, RootStyle<DrylScrollArea>("[style]"));
    }

    [Fact]
    public void The_style_attribute_binds_to_the_parameter_and_never_reaches_the_splat()
    {
        var cut = Render<DrylStack>(p => p.AddUnmatched("style", Extra));

        Assert.Equal(Extra, cut.Instance.Style);
        Assert.True(cut.Instance.AdditionalAttributes is null
                    || !cut.Instance.AdditionalAttributes.ContainsKey("style"));
    }
}
