using Microsoft.Playwright;

namespace DRYL.BrowserTests;

[Collection("Browser")]
public sealed class FocusTests(BrowserFixture browser)
{
    public static TheoryData<string, bool> Modes
    {
        get
        {
            var data = new TheoryData<string, bool> { { "dark", false }, { "light", false } };
            // Forced-color emulation is intentionally a Chromium-specific gate.
            var engine = Environment.GetEnvironmentVariable("DRYL_BROWSER") ?? "chromium";
            if (engine is "chromium" or "chrome" or "msedge") { data.Add("dark", true); data.Add("light", true); }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public Task Keyboard_controls_keep_visible_focus_and_real_selection(string mode, bool forced) =>
        browser.RunAsync($"{nameof(Keyboard_controls_keep_visible_focus_and_real_selection)}-{forced}", mode, async page =>
        {
            Assert.Equal(forced, await page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches"));
            await page.Locator("#focus-before").FocusAsync();
            await page.Keyboard.PressAsync("Tab");
            var headers = page.Locator("#focus-scene .step-header");
            await Focus(headers.First, forced);
            await page.Locator("#focus-scene").ScreenshotAsync(new() { Path = Path.Combine(browser.Artifacts, $"visual-focus-{mode}-{forced}.png") });
            await page.Keyboard.PressAsync("Tab");
            await Focus(headers.Nth(1), forced);
            await page.Keyboard.PressAsync("Enter");
            await Assertions.Expect(page.Locator("#step-second")).ToBeVisibleAsync();
            await page.Keyboard.PressAsync("Tab");
            var input = page.GetByLabel("Name", new() { Exact = true });
            await Focus(input, forced);
            await page.Keyboard.TypeAsync("Ada");
            await page.Keyboard.PressAsync("Tab");
            var textarea = page.GetByLabel("Notes", new() { Exact = true });
            await Focus(textarea, forced);
            await page.Keyboard.TypeAsync("First line");
            await page.Keyboard.PressAsync("Enter");
            await page.Keyboard.TypeAsync("Second line");
            await Assertions.Expect(textarea).ToHaveValueAsync("First line\nSecond line");
            await page.Keyboard.PressAsync("Tab");
            var select = page.GetByRole(AriaRole.Combobox, new() { Name = "Environment", Exact = true });
            await Focus(select, forced);
            await page.Keyboard.PressAsync("ArrowDown");
            await Assertions.Expect(page.GetByRole(AriaRole.Listbox)).ToBeVisibleAsync();
            await page.Keyboard.PressAsync("ArrowDown");
            await page.Keyboard.PressAsync("Enter");
            await Assertions.Expect(select).ToHaveTextAsync("Beta");
            await Assertions.Expect(page.GetByRole(AriaRole.Listbox)).ToHaveCountAsync(0);
            await Focus(select, forced);
            await page.Keyboard.PressAsync("Tab");
            await Assertions.Expect(page.Locator("#focus-after")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Shift+Tab");
            await Focus(select, forced);
            await page.Keyboard.PressAsync("ArrowDown");
            await Assertions.Expect(page.GetByRole(AriaRole.Listbox)).ToBeVisibleAsync();
            await page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(page.GetByRole(AriaRole.Listbox)).ToHaveCountAsync(0);
            await Focus(select, forced);

            await page.Locator("#dialog-open").FocusAsync();
            await page.Keyboard.PressAsync("Enter");
            var close = page.Locator(".dialog-close");
            await Focus(close, forced);
            await page.Locator(".dialog").ScreenshotAsync(new() { Path = Path.Combine(browser.Artifacts, $"visual-dialog-{mode}-{forced}.png") });
            await page.Keyboard.PressAsync("Enter");
            await Assertions.Expect(page.Locator(".dialog-backdrop")).ToHaveCountAsync(0);
            await Assertions.Expect(page.Locator("#dialog-open")).ToBeFocusedAsync();
        }, ReducedMotion.Reduce, forced ? ForcedColors.Active : ForcedColors.None);

    [Theory]
    [MemberData(nameof(Modes))]
    public Task Script_focused_heading_and_main_show_no_ring(string mode, bool forced) =>
        browser.RunAsync($"{nameof(Script_focused_heading_and_main_show_no_ring)}-{forced}", mode, async page =>
        {
            // Keyboard modality first, so a script focus matches :focus-visible the way it
            // does after FocusOnNavigate on a fresh page.
            await page.Locator("#focus-before").FocusAsync();
            await page.Keyboard.PressAsync("Tab");

            var heading = page.Locator("h1").First;

            // In the tab order a heading is a focus stop like any other: the ring stays.
            await heading.EvaluateAsync("el => { el.setAttribute('tabindex', '0'); el.focus(); }");
            await Assertions.Expect(heading).ToBeFocusedAsync();
            Assert.True(await HasOutline(heading), "A heading in the tab order must keep the accent ring.");

            // What FocusOnNavigate does: tabindex=-1, then focus.
            await heading.EvaluateAsync("el => { el.blur(); el.setAttribute('tabindex', '-1'); el.focus(); }");
            await Assertions.Expect(heading).ToBeFocusedAsync();
            Assert.False(await HasOutline(heading), "A script-focused heading must not draw a ring.");

            var main = page.Locator("main").First;
            await main.EvaluateAsync("el => { el.setAttribute('tabindex', '-1'); el.focus(); }");
            await Assertions.Expect(main).ToBeFocusedAsync();
            Assert.False(await HasOutline(main), "A script-focused main landmark must not draw a ring.");
        }, ReducedMotion.Reduce, forced ? ForcedColors.Active : ForcedColors.None);

    private static Task<bool> HasOutline(ILocator locator) =>
        locator.EvaluateAsync<bool>("el => { const s = getComputedStyle(el); return s.outlineStyle !== 'none' && parseFloat(s.outlineWidth) > 0; }");

    private static async Task Focus(ILocator locator, bool forced)
    {
        await Assertions.Expect(locator).ToBeFocusedAsync();
        Assert.True(await locator.EvaluateAsync<bool>("el => el.matches(':focus-visible')"));
        var outline = await locator.EvaluateAsync<bool>("el => { const s = getComputedStyle(el); return s.outlineStyle !== 'none' && parseFloat(s.outlineWidth) > 0 && s.outlineColor !== 'rgba(0, 0, 0, 0)'; }");
        if (forced) Assert.True(outline, "Forced colors must retain a visible outline when shadows disappear.");
        else Assert.True(outline || await locator.EvaluateAsync<bool>("el => getComputedStyle(el).boxShadow !== 'none'"));
    }
}
