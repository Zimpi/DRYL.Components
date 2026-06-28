using DRYL.Components.Theming;

namespace DRYL.Components.Tests.Theming;

public class DrylThemeServiceTests
{
    [Fact]
    public void Current_defaults_to_Nebula()
    {
        var svc = new DrylThemeService();
        Assert.Equal(DrylThemes.Nebula, svc.Current);
    }

    [Fact]
    public async Task SetThemeAsync_updates_current_and_raises_event()
    {
        var svc = new DrylThemeService();
        var raised = 0;
        svc.OnThemeChanged += () => { raised++; return Task.CompletedTask; };

        await svc.SetThemeAsync(DrylThemes.Ember);

        Assert.Equal(DrylThemes.Ember, svc.Current);
        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task SetAccentAsync_replaces_only_the_accent()
    {
        var svc = new DrylThemeService();
        await svc.SetThemeAsync(DrylThemes.Ember);

        await svc.SetAccentAsync("#111111", "#222222");

        Assert.Equal(new DrylAccent("#111111", "#222222"), svc.Current.Accent);
        // Ember had no semantic overrides, but the record-with preserves everything else.
        Assert.Equal(DrylThemes.Ember.Semantic, svc.Current.Semantic);
    }

    [Fact]
    public async Task SetThemeAsync_null_throws()
    {
        var svc = new DrylThemeService();
        await Assert.ThrowsAsync<ArgumentNullException>(() => svc.SetThemeAsync(null!));
    }
}
