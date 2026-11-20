using Bunit;
using DRYL.Components;
using DRYL.Components.Theming;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests.Theming;

public class DrylThemeProviderTests : BunitContext
{
    public DrylThemeProviderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<IDrylThemeService, DrylThemeService>();
    }

    [Fact]
    public void Renders_default_seeds_into_root_style_when_no_theme()
    {
        var cut = Render<DrylThemeProvider>();

        var style = cut.Find("style");
        Assert.Contains(":root", style.TextContent);
        Assert.Contains("--accent-a:#7c5cff;", style.TextContent);
        Assert.Contains("--accent-b:#22d3ee;", style.TextContent);
    }

    [Fact]
    public void Applies_supplied_theme_seeds()
    {
        var cut = Render<DrylThemeProvider>(ps => ps.Add(p => p.Theme, DrylThemes.Ember));

        var style = cut.Find("style");
        Assert.Contains("--accent-a:#f59e0b;", style.TextContent);
        Assert.Contains("--accent-b:#f43f5e;", style.TextContent);
    }

    [Fact]
    public async Task Reacts_to_runtime_theme_change()
    {
        var svc = Services.GetRequiredService<IDrylThemeService>();
        var cut = Render<DrylThemeProvider>();

        await cut.InvokeAsync(() => svc.SetThemeAsync(DrylThemes.Verdant));

        var style = cut.Find("style");
        Assert.Contains("--accent-a:#34d399;", style.TextContent);
    }

    [Fact]
    public void Renders_prepaint_mode_restore_script()
    {
        var cut = Render<DrylThemeProvider>();

        Assert.Contains("dryl-color-mode", cut.Markup);          // localStorage key
        Assert.Contains("data-dryl-mode", cut.Markup);           // attribute contract
    }

    [Fact]
    public void Mode_parameter_is_baked_into_the_restore_script()
    {
        var cut = Render<DrylThemeProvider>(ps => ps.Add(p => p.Mode, DrylColorMode.Light));

        Assert.Contains("var p='light'", cut.Markup);
    }

    [Fact]
    public async Task Runtime_mode_change_invokes_applyMode_with_persist()
    {
        var svc = Services.GetRequiredService<IDrylThemeService>();
        var cut = Render<DrylThemeProvider>();
        var invocation = JSInterop.SetupVoid("dryl.theme.applyMode", "dark", true).SetVoidResult();

        await cut.InvokeAsync(() => svc.SetModeAsync(DrylColorMode.Dark));

        invocation.VerifyInvoke("dryl.theme.applyMode");
    }

    [Fact]
    public void A_changed_Theme_parameter_is_applied()
    {
        var svc = Services.GetRequiredService<IDrylThemeService>();
        var cut = Render<DrylThemeProvider>(ps => ps.Add(p => p.Theme, DrylThemes.Ember));

        cut.Render(ps => ps.Add(p => p.Theme, DrylThemes.Verdant));

        Assert.Equal(DrylThemes.Verdant, svc.Current);
        cut.WaitForAssertion(() => Assert.Contains("--accent-a:#34d399;", cut.Find("style").TextContent));
    }

    [Fact]
    public async Task A_runtime_theme_survives_a_render_with_the_unchanged_parameter()
    {
        var svc = Services.GetRequiredService<IDrylThemeService>();
        var cut = Render<DrylThemeProvider>(ps => ps.Add(p => p.Theme, DrylThemes.Ember));
        await cut.InvokeAsync(() => svc.SetThemeAsync(DrylThemes.Verdant));

        cut.Render(ps => ps.Add(p => p.Theme, DrylThemes.Ember));

        Assert.Equal(DrylThemes.Verdant, svc.Current);
    }

    [Fact]
    public void An_equal_theme_built_anew_is_not_applied_again()
    {
        var svc = Services.GetRequiredService<IDrylThemeService>();
        var changes = 0;
        var cut = Render<DrylThemeProvider>(ps => ps.Add(p => p.Theme, DrylThemes.Ember with { }));
        svc.OnThemeChanged += () => { changes++; return Task.CompletedTask; };

        cut.Render(ps => ps.Add(p => p.Theme, DrylThemes.Ember with { }));

        Assert.Equal(0, changes);
    }

    [Fact]
    public void A_changed_Mode_parameter_has_no_effect_after_startup()
    {
        var svc = Services.GetRequiredService<IDrylThemeService>();
        var cut = Render<DrylThemeProvider>(ps => ps.Add(p => p.Mode, DrylColorMode.Dark));
        cut.WaitForAssertion(() => Assert.Equal(DrylColorMode.Dark, svc.CurrentMode));

        cut.Render(ps => ps.Add(p => p.Mode, DrylColorMode.Light));

        Assert.Equal(DrylColorMode.Dark, svc.CurrentMode);
    }

    [Fact]
    public void Without_a_nonce_neither_element_carries_the_attribute()
    {
        var cut = Render<DrylThemeProvider>();

        Assert.DoesNotContain("nonce", cut.Markup);
    }

    [Fact]
    public void A_nonce_is_stamped_on_style_and_script()
    {
        var cut = Render<DrylThemeProvider>(ps => ps.Add(p => p.Nonce, "r4nd0m"));

        Assert.Equal("r4nd0m", cut.Find("style").GetAttribute("nonce"));
        Assert.Contains("<script nonce=\"r4nd0m\">", cut.Markup);
    }

    [Fact]
    public void A_nonce_cannot_break_out_of_its_attribute()
    {
        var cut = Render<DrylThemeProvider>(ps => ps.Add(p => p.Nonce, "x\"><img src=x>"));

        // The value stays inside both attributes: no element is born from it.
        Assert.Empty(cut.FindAll("img"));
        Assert.Contains("nonce=\"x&quot;&gt;&lt;img src=x&gt;\">", cut.Markup);   // the script tag, encoded
    }
}
