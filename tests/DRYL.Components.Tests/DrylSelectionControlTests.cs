using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// I39 "Tinte": the selection controls carry a touched flag after a user change (the glow
/// pulse is gated on it), render their label through shared classes instead of inline
/// styles, and the CSS replaces <c>transition: all</c> with an explicit list.
/// </summary>
public class DrylSelectionControlTests : BunitContext
{
    [Fact]
    public void A_checkbox_is_not_touched_on_first_render()
    {
        var cut = Render<DrylCheckbox>(ps => ps.Add(p => p.Value, true).Add(p => p.Label, "Terms"));

        Assert.False(cut.Find("input.checkbox").HasAttribute("data-touched"));
    }

    [Fact]
    public void A_checkbox_is_touched_after_the_user_changes_it()
    {
        var value = false;
        var cut = Render<DrylCheckbox>(ps => ps
            .Add(p => p.Value, value)
            .Add(p => p.ValueChanged, v => value = v)
            .Add(p => p.Label, "Terms"));

        cut.Find("input.checkbox").Change(true);

        Assert.True(value);
        Assert.True(cut.Find("input.checkbox").HasAttribute("data-touched"));
    }

    [Fact]
    public void A_checkbox_label_uses_the_shared_choice_classes()
    {
        var cut = Render<DrylCheckbox>(ps => ps.Add(p => p.Label, "Terms"));

        var row = cut.Find("label");
        Assert.Contains("choice", row.ClassList);
        Assert.Null(row.GetAttribute("style"));
        var label = cut.Find(".choice-label");
        Assert.Equal("Terms", label.TextContent);
        Assert.Null(label.GetAttribute("style"));
    }

    [Fact]
    public void A_disabled_checkbox_marks_its_row()
    {
        var cut = Render<DrylCheckbox>(ps => ps.Add(p => p.Label, "Terms").Add(p => p.Disabled, true));

        Assert.Contains("choice--disabled", cut.Find("label").ClassList);
        Assert.True(cut.Find("input").HasAttribute("disabled"));
    }

    [Fact]
    public void A_toggle_is_touched_only_after_the_user_changes_it()
    {
        var value = false;
        var cut = Render<DrylToggle>(ps => ps
            .Add(p => p.Value, value)
            .Add(p => p.ValueChanged, v => value = v)
            .Add(p => p.Label, "Dark mode"));

        Assert.False(cut.Find("input.toggle").HasAttribute("data-touched"));
        cut.Find("input.toggle").Change(true);

        Assert.True(value);
        Assert.True(cut.Find("input.toggle").HasAttribute("data-touched"));
    }

    [Fact]
    public void A_toggle_row_puts_label_and_switch_at_the_ends()
    {
        var cut = Render<DrylToggle>(ps => ps.Add(p => p.Label, "Dark mode"));

        var row = cut.Find("label");
        Assert.Contains("choice", row.ClassList);
        Assert.Contains("choice--end", row.ClassList);
        Assert.Null(row.GetAttribute("style"));
        Assert.Equal("Dark mode", cut.Find(".choice-label").TextContent.Trim());
    }

    [Fact]
    public void A_radio_is_touched_once_the_user_picks_it()
    {
        var value = "free";
        var cut = Render<DrylRadioGroup<string>>(ps => ps
            .Add(p => p.Value, value)
            .Add(p => p.ValueChanged, v => value = v)
            .AddChildContent<DrylRadio<string>>(r => r.Add(p => p.Value, "free").Add(p => p.Label, "Free"))
            .AddChildContent<DrylRadio<string>>(r => r.Add(p => p.Value, "pro").Add(p => p.Label, "Pro")));

        Assert.Empty(cut.FindAll(".radio-input[data-touched]"));

        cut.FindAll(".radio-input")[1].Change("pro");

        Assert.Equal("pro", value);
        var touched = cut.FindAll(".radio-input[data-touched]");
        Assert.Single(touched);
        Assert.Equal("pro", touched[0].GetAttribute("value"));
    }

    [Fact]
    public void The_stylesheet_names_its_transitions_and_gates_the_pulse()
    {
        var css = ReadDrylCss();
        var checkbox = Rule(css, ".checkbox {");

        Assert.DoesNotContain("transition: all", checkbox);
        Assert.Contains("border-radius: var(--r-xs)", checkbox);
        Assert.Contains(".checkbox[data-touched]:checked { animation: dryl-choice-pulse", css);
        Assert.Contains(".toggle[data-touched]:checked { animation: dryl-toggle-pulse", css);
        Assert.Contains(".radio-input[data-touched]:checked + .radio-control { animation: dryl-choice-pulse", css);
        // The fill is its own layer, so it can fade — the checked state paints no gradient itself.
        Assert.DoesNotContain("background", Rule(css, ".checkbox:checked {"));
        Assert.DoesNotContain("background", Rule(css, ".toggle:checked {"));
    }

    private static string Rule(string css, string selector)
    {
        var start = css.IndexOf(selector, StringComparison.Ordinal);
        Assert.True(start >= 0, $"rule {selector} not found");
        return css[start..css.IndexOf('}', start)];
    }

    private static string ReadDrylCss()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DRYL.slnx"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(dir!.FullName, "code", "DRYL.Components", "wwwroot", "dryl.css"));
    }
}
