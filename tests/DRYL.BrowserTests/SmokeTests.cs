using Microsoft.Playwright;

namespace DRYL.BrowserTests;

[Collection("Browser")]
public sealed class SmokeTests(BrowserFixture browser)
{
    [Theory]
    [InlineData("dark")]
    [InlineData("light")]
    public Task Real_components_and_both_stylesheet_surfaces_load(string mode) =>
        browser.RunAsync(nameof(Real_components_and_both_stylesheet_surfaces_load), mode, async page =>
        {
            await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("I13 regression host");
            Assert.True(await page.EvaluateAsync<bool>("() => typeof window.dryl?.motion?.onExit === 'function'"));
            Assert.NotEmpty(await page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).getPropertyValue('--fg').trim()"));
            // .step-header's gap comes from isolated CSS, not the global stylesheet.
            Assert.NotEqual("normal", await page.Locator("#focus-scene .step-header").First.EvaluateAsync<string>("el => getComputedStyle(el).gap"));
            await Assertions.Expect(page.GetByLabel("Name", new() { Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#table-scene table")).ToBeVisibleAsync();
            // Existing DOM survives a disconnected circuit. Require a real event/render roundtrip.
            await page.Locator("#focus-scene .step-header").Nth(1).ClickAsync();
            await Assertions.Expect(page.Locator("#step-second")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#focus-values")).ToContainTextAsync("second");
        });
}
