using DRYL.Components.Theming;

namespace DRYL.Components.Tests.Theming;

public class DrylThemeServiceModeTests
{
    [Fact]
    public void CurrentMode_defaults_to_System()
    {
        var svc = new DrylThemeService();
        Assert.Equal(DrylColorMode.System, svc.CurrentMode);
    }

    [Fact]
    public async Task SetModeAsync_updates_CurrentMode_and_raises_event()
    {
        var svc = new DrylThemeService();
        var raised = 0;
        svc.OnModeChanged += () => { raised++; return Task.CompletedTask; };

        await svc.SetModeAsync(DrylColorMode.Light);

        Assert.Equal(DrylColorMode.Light, svc.CurrentMode);
        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task SetModeAsync_is_a_noop_for_the_current_mode()
    {
        var svc = new DrylThemeService();
        var raised = 0;
        svc.OnModeChanged += () => { raised++; return Task.CompletedTask; };

        await svc.SetModeAsync(DrylColorMode.System);

        Assert.Equal(0, raised);
    }
}
