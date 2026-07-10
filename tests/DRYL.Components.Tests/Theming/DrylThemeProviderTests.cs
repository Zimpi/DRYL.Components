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
}
