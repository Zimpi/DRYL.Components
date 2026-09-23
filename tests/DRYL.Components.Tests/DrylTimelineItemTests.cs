using Bunit;
using DRYL.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace DRYL.Components.Tests;

/// <summary>
/// A timeline item can be a numbered step the user jumps to — "go here first, then there" —
/// and without the new parameters it is exactly the static event it was.
/// </summary>
public class DrylTimelineItemTests : BunitContext
{
    public DrylTimelineItemTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void A_static_item_renders_no_button_and_no_current_state()
    {
        var cut = Render<DrylTimelineItem>(p => p.Add(x => x.Title, "Arrived"));

        Assert.Empty(cut.FindAll("button"));
        Assert.Null(cut.Find(".timeline-item").GetAttribute("aria-current"));
        Assert.NotNull(cut.Find(".timeline-dot"));
    }

    [Fact]
    public void Number_renders_in_the_marker_instead_of_the_icon()
    {
        var cut = Render<DrylTimelineItem>(p => p
            .Add(x => x.Number, 2)
            .Add(x => x.Icon, "MapPin"));

        Assert.Equal("2", cut.Find(".timeline-marker .timeline-number").TextContent);
        Assert.Empty(cut.FindAll(".timeline-marker svg"));
        Assert.Empty(cut.FindAll(".timeline-dot"));
    }

    [Fact]
    public void MarkerContent_wins_over_Number_and_Icon()
    {
        var cut = Render<DrylTimelineItem>(p => p
            .Add(x => x.MarkerContent, (RenderFragment)(b => b.AddContent(0, "B")))
            .Add(x => x.Number, 2)
            .Add(x => x.Icon, "MapPin"));

        Assert.Equal("B", cut.Find(".timeline-marker .timeline-marker-content").TextContent);
        Assert.Empty(cut.FindAll(".timeline-number"));
    }

    [Fact]
    public void OnClick_makes_the_row_one_native_button_named_by_the_title()
    {
        var clicks = 0;
        var cut = Render<DrylTimelineItem>(p => p
            .Add(x => x.Title, "Sandy Shores")
            .Add(x => x.OnClick, (MouseEventArgs _) => clicks++));

        var button = cut.Find("button.timeline-hit");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("Sandy Shores", button.GetAttribute("aria-label"));
        Assert.Contains("is-interactive", cut.Find(".timeline-item").ClassList);

        button.Click();
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void AriaLabel_names_a_step_without_a_title()
    {
        var cut = Render<DrylTimelineItem>(p => p
            .Add(x => x.Number, 3)
            .Add(x => x.AriaLabel, "Stopp 3")
            .Add(x => x.OnClick, (MouseEventArgs _) => { }));

        Assert.Equal("Stopp 3", cut.Find("button.timeline-hit").GetAttribute("aria-label"));
    }

    [Fact]
    public void Active_on_an_interactive_item_marks_its_button_as_the_current_step()
    {
        var cut = Render<DrylTimelineItem>(p => p
            .Add(x => x.Title, "Paleto Bay")
            .Add(x => x.Active, true)
            .Add(x => x.OnClick, (MouseEventArgs _) => { }));

        Assert.Equal("step", cut.Find("button.timeline-hit").GetAttribute("aria-current"));
        Assert.Null(cut.Find(".timeline-item").GetAttribute("aria-current"));
        Assert.Contains("is-active", cut.Find(".timeline-item").ClassList);
    }

    [Fact]
    public void Active_on_a_static_item_marks_the_row_as_the_current_step()
    {
        var cut = Render<DrylTimelineItem>(p => p
            .Add(x => x.Title, "Paleto Bay")
            .Add(x => x.Active, true));

        Assert.Equal("step", cut.Find(".timeline-item").GetAttribute("aria-current"));
    }

    [Fact]
    public void Disabled_disables_the_button()
    {
        var cut = Render<DrylTimelineItem>(p => p
            .Add(x => x.Title, "Closed road")
            .Add(x => x.Disabled, true)
            .Add(x => x.OnClick, (MouseEventArgs _) => { }));

        Assert.True(cut.Find("button.timeline-hit").HasAttribute("disabled"));
    }

    [Fact]
    public void Class_is_merged_rather_than_replacing_the_item_class()
    {
        var cut = Render<DrylTimelineItem>(p => p.Add(x => x.Class, "mine"));

        var classes = cut.Find("[role=listitem]").ClassList;
        Assert.Contains("timeline-item", classes);
        Assert.Contains("mine", classes);
    }
}
