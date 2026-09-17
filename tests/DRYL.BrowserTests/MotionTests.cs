using Microsoft.Playwright;

namespace DRYL.BrowserTests;

[Collection("Browser")]
public sealed class MotionTests(BrowserFixture browser)
{
    [Theory]
    [InlineData("dark", false)]
    [InlineData("light", false)]
    [InlineData("dark", true)]
    [InlineData("light", true)]
    public Task All_scoped_decorations_honor_initial_and_live_reduced_motion(string mode, bool initial) =>
        browser.RunAsync($"{nameof(All_scoped_decorations_honor_initial_and_live_reduced_motion)}-{initial}", mode, async page =>
        {
            if (!initial)
            {
                Assert.NotEqual("none", await page.Locator("#spinner-sample").EvaluateAsync<string>("el => getComputedStyle(el).animationName"));
                Assert.NotEqual("none", await page.Locator("[data-aura=Comet] .ai-aura-comet").EvaluateAsync<string>("el => getComputedStyle(el, '::before').animationName"));
                await page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
            }
            foreach (var state in new[] { "Active", "Thinking", "Streaming", "Generated" })
            {
                await page.Locator("#aura-state").SelectOptionAsync(state);
                var modifier = state == "Active" ? ".ai-aura" : $".ai-{state.ToLowerInvariant()}";
                await Assertions.Expect(page.Locator($"[data-aura=Comet] {modifier}").First).ToBeVisibleAsync();
                var moving = await page.EvaluateAsync<string[]>("""
                    () => {
                        const elements = document.querySelectorAll('#aura-scene .ai-aura, #aura-scene [class^="ai-aura-"], #spinner-sample, #fade-sample, #stagger-sample > *, #ambient-sample, #ambient-sample .orb, #focus-scene .step-panel');
                        const result = [];
                        for (const el of elements) for (const pseudo of [null, '::before', '::after']) {
                            const s = getComputedStyle(el, pseudo);
                            if (pseudo && (s.content === 'none' || s.content === 'normal')) continue;
                            if (s.animationName.split(',').some(name => name.trim() !== 'none'))
                                result.push(`${el.id || el.className}${pseudo || ''}: ${s.animationName}`);
                        }
                        return result;
                    }
                    """);
                Assert.Empty(moving);
                Assert.Equal("1", await page.Locator("#fade-sample").EvaluateAsync<string>("el => getComputedStyle(el).opacity"));
            }
            await page.Locator("#aura-state").SelectOptionAsync("None");
            await Assertions.Expect(page.Locator("#aura-scene .ai-aura-ring")).ToHaveCountAsync(0);
            await page.Locator("#dialog-open").ClickAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
            Assert.Equal("none", await page.Locator(".dialog-backdrop").EvaluateAsync<string>("el => getComputedStyle(el).animationName"));
            Assert.Equal("0s", await page.Locator(".dialog-backdrop").EvaluateAsync<string>("el => getComputedStyle(el).transitionDuration"));
            Assert.Equal("none", await page.Locator(".dialog").EvaluateAsync<string>("el => getComputedStyle(el).animationName"));
            await page.Locator("#dialog-cancel").ClickAsync();
            await Assertions.Expect(page.Locator(".dialog-backdrop")).ToHaveCountAsync(0);
        }, initial ? ReducedMotion.Reduce : ReducedMotion.NoPreference);

    [Theory]
    [InlineData("dark", "missing")]
    [InlineData("light", "missing")]
    [InlineData("dark", "cancel")]
    [InlineData("light", "cancel")]
    [InlineData("dark", "preference")]
    [InlineData("light", "preference")]
    public Task Interrupted_presence_exit_completes_once(string mode, string interruption) =>
        browser.RunAsync($"{nameof(Interrupted_presence_exit_completes_once)}-{interruption}", mode, async page =>
        {
            if (interruption == "missing") await page.AddStyleTagAsync(new() { Content = "#presence-scene .presence-exit { animation: none !important; }" });
            await page.Locator("#presence-toggle").ClickAsync();
            if (interruption != "missing")
            {
                await Assertions.Expect(page.Locator("#presence-scene .presence-exit")).ToHaveCountAsync(1);
                if (interruption == "preference") await page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
                else Assert.True(await page.Locator("#presence-scene .presence-exit").EvaluateAsync<bool>("el => { const a = el.getAnimations(); a.forEach(item => item.cancel()); return a.length > 0; }"));
            }
            await Assertions.Expect(page.Locator("#presence-child")).ToHaveCountAsync(0);
            await Assertions.Expect(page.Locator("#presence-exits")).ToHaveTextAsync("1");
            await page.Locator("#presence-toggle").ClickAsync();
            await Assertions.Expect(page.Locator("#presence-child")).ToBeVisibleAsync();
            await page.Locator("#presence-remove").ClickAsync();
            await Assertions.Expect(page.Locator("#presence-child")).ToHaveCountAsync(0);
            await Assertions.Expect(page.Locator("#presence-exits")).ToHaveTextAsync("1");
        });

    [Theory]
    [InlineData("dark")]
    [InlineData("light")]
    public Task Reopening_replaces_the_exit_and_normal_motion_still_runs(string mode) =>
        browser.RunAsync(nameof(Reopening_replaces_the_exit_and_normal_motion_still_runs), mode, async page =>
        {
            await page.Locator("#presence-toggle").ClickAsync();
            await Assertions.Expect(page.Locator("#presence-scene .presence-exit")).ToHaveCountAsync(1);
            // Reopen in the same browser turn that observes the running exit.
            // A second actionability wait can legitimately outlast --dur-slow
            // on WebKit and would then test two completed closes instead.
            Assert.True(await page.Locator("#presence-scene .presence-exit").EvaluateAsync<bool>("el => { const running = el.getAnimations().some(a => a.playState === 'running'); document.querySelector('#presence-toggle').click(); return running; }"));
            await Assertions.Expect(page.Locator("#presence-scene .presence-enter")).ToHaveCountAsync(1);
            await Assertions.Expect(page.Locator("#presence-child")).ToBeVisibleAsync();
            await page.Locator("#presence-toggle").ClickAsync();
            await Assertions.Expect(page.Locator("#presence-child")).ToHaveCountAsync(0);
            await Assertions.Expect(page.Locator("#presence-exits")).ToHaveTextAsync("1");
            await page.Locator("#dialog-open").ClickAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
            Assert.NotEqual("none", await page.Locator(".dialog").EvaluateAsync<string>("el => getComputedStyle(el).animationName"));
            await page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(page.Locator(".dialog-layer")).ToHaveCountAsync(0);
            Assert.False(await page.Locator("body").EvaluateAsync<bool>("el => el.classList.contains('dryl-scroll-locked')"));
            await Assertions.Expect(page.Locator("#dialog-open")).ToBeFocusedAsync();
        });
}
