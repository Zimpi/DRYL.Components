using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Structure tests for <see cref="DrylReconnectModal"/>. Which block shows is CSS keyed
/// on the framework's state classes (and on <c>data-pause-origin</c>), which bUnit does
/// not evaluate — these tests pin the markup that CSS and the framework rely on.
/// </summary>
public class DrylReconnectModalTests : BunitContext
{
    [Fact]
    public void Root_is_the_element_the_framework_looks_for()
    {
        var cut = Render<DrylReconnectModal>();

        var root = cut.Find("#components-reconnect-modal");
        Assert.Contains("reconnect-overlay", root.ClassList);
        Assert.Equal("alertdialog", root.GetAttribute("role"));
    }

    [Fact]
    public void Renders_all_five_state_blocks()
    {
        var cut = Render<DrylReconnectModal>();

        foreach (var block in new[] { "reconnecting", "failed", "paused", "resume-failed", "rejected" })
            Assert.Single(cut.FindAll($".reconnect-card > .reconnect-{block}"));
    }

    [Fact]
    public void Paused_block_offers_a_native_resume()
    {
        var cut = Render<DrylReconnectModal>(ps => ps
            .Add(p => p.PausedText, "Pausiert.")
            .Add(p => p.ResumeButtonText, "Fortsetzen"));

        var block = cut.Find(".reconnect-paused");
        Assert.Equal("alert", block.GetAttribute("role"));
        Assert.Contains("Pausiert.", block.TextContent);

        var button = block.QuerySelector("button")!;
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("Fortsetzen", button.TextContent.Trim());
        Assert.Contains("Blazor.resumeCircuit()", button.GetAttribute("onclick"));
        Assert.Contains("location.reload()", button.GetAttribute("onclick"));
        Assert.Contains("components-reconnect-resume-failed", button.GetAttribute("onclick"));
    }

    [Fact]
    public void Resume_failed_block_offers_a_native_reload()
    {
        var cut = Render<DrylReconnectModal>(ps => ps
            .Add(p => p.ResumeFailedText, "Fortsetzen fehlgeschlagen.")
            .Add(p => p.ReloadButtonText, "Neu laden"));

        var block = cut.Find(".reconnect-resume-failed");
        Assert.Equal("alert", block.GetAttribute("role"));
        Assert.Contains("Fortsetzen fehlgeschlagen.", block.TextContent);

        var button = block.QuerySelector("button")!;
        Assert.Equal("Neu laden", button.TextContent.Trim());
        Assert.Equal("location.reload()", button.GetAttribute("onclick"));
    }

    [Fact]
    public void Failed_block_keeps_its_native_reconnect()
    {
        var cut = Render<DrylReconnectModal>();

        Assert.Equal("Blazor.reconnect()", cut.Find(".reconnect-failed button").GetAttribute("onclick"));
    }

    [Fact]
    public void Class_is_merged_onto_the_overlay()
    {
        var cut = Render<DrylReconnectModal>(ps => ps.Add(p => p.Class, "components-reconnect-paused"));

        var root = cut.Find("#components-reconnect-modal");
        Assert.Contains("reconnect-overlay", root.ClassList);
        Assert.Contains("components-reconnect-paused", root.ClassList);
    }

    [Fact]
    public void Attempt_counter_can_be_turned_off()
    {
        var cut = Render<DrylReconnectModal>(ps => ps.Add(p => p.ShowAttemptCounter, false));

        Assert.Empty(cut.FindAll("#components-reconnect-current-attempt"));
        Assert.Empty(cut.FindAll("#components-seconds-to-next-attempt"));
    }
}
