using Bunit;
using DRYL.Components;
using DRYL.Components.Theming;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests.Theming;

public class DrylColorModeToggleTests : BunitContext
{
    public DrylColorModeToggleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<IDrylThemeService, DrylThemeService>();
    }

    [Fact]
    public void Renders_button_with_mode_label()
    {
        var cut = Render<DrylColorModeToggle>();
        var btn = cut.Find("button");

        Assert.Contains("System", btn.GetAttribute("aria-label"));
    }

    [Fact]
    public async Task Click_cycles_System_Light_Dark_System()
    {
        var svc = Services.GetRequiredService<IDrylThemeService>();
        var cut = Render<DrylColorModeToggle>();

        await cut.Find("button").ClickAsync(new());
        Assert.Equal(DrylColorMode.Light, svc.CurrentMode);

        await cut.Find("button").ClickAsync(new());
        Assert.Equal(DrylColorMode.Dark, svc.CurrentMode);

        await cut.Find("button").ClickAsync(new());
        Assert.Equal(DrylColorMode.System, svc.CurrentMode);
    }

    [Fact]
    public async Task State_class_follows_the_chosen_mode()
    {
        var cut = Render<DrylColorModeToggle>();
        Assert.Contains("is-system", cut.Find("button").ClassList);

        await cut.Find("button").ClickAsync(new());
        Assert.Contains("is-light", cut.Find("button").ClassList);
    }
}
