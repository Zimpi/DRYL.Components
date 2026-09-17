using System.Text.Json;
using Microsoft.Playwright;

namespace DRYL.BrowserTests;

[Collection("Browser")]
public sealed class ContrastTests(BrowserFixture browser)
{
    [Theory]
    [InlineData("dark")]
    [InlineData("light")]
    public Task Composed_badge_text_meets_contrast_and_dots_keep_semantics(string mode) =>
        browser.RunAsync(nameof(Composed_badge_text_meets_contrast_and_dots_keep_semantics), mode, async page =>
        {
            var results = await page.EvaluateAsync<JsonElement>("""
                () => {
                    const context = document.createElement('canvas').getContext('2d', { willReadFrequently: true });
                    const rgba = color => {
                        context.clearRect(0, 0, 1, 1); context.fillStyle = color; context.fillRect(0, 0, 1, 1);
                        const pixel = Array.from(context.getImageData(0, 0, 1, 1).data);
                        return [pixel[0], pixel[1], pixel[2], pixel[3] / 255];
                    };
                    const over = (a, b) => {
                        const alpha = a[3] + b[3] * (1 - a[3]);
                        return [0, 1, 2].map(i => alpha ? (a[i] * a[3] + b[i] * b[3] * (1 - a[3])) / alpha : 0).concat(alpha);
                    };
                    const luminance = color => color.slice(0, 3).map(c => c / 255)
                        .map(c => c <= .04045 ? c / 12.92 : ((c + .055) / 1.055) ** 2.4)
                        .reduce((sum, c, i) => sum + c * [.2126, .7152, .0722][i], 0);
                    return Array.from(document.querySelectorAll('#badge-scene .badge')).map(el => {
                        const chain = []; for (let node = el; node; node = node.parentElement) chain.unshift(node);
                        let background = [0, 0, 0, 0];
                        for (const node of chain) {
                            const style = getComputedStyle(node);
                            // The fixture deliberately uses uniform surfaces. A gradient,
                            // opacity group or blend requires a different pixel-level test.
                            if (style.backgroundImage !== 'none' || style.opacity !== '1' || style.mixBlendMode !== 'normal')
                                throw new Error(`Unsupported contrast composition at ${node.className}`);
                            background = over(rgba(style.backgroundColor), background);
                        }
                        if (background[3] !== 1) throw new Error('Badge has no opaque ancestor ground');
                        const style = getComputedStyle(el), foreground = over(rgba(style.color), background);
                        const a = luminance(foreground), b = luminance(background);
                        const kind = el.dataset.kind, semantic = ['success', 'warning', 'danger'].includes(kind);
                        const dot = rgba(getComputedStyle(el, '::before').color);
                        const expectedDot = rgba(semantic ? style.getPropertyValue(`--${kind}`).trim() : style.color);
                        return { surface: el.closest('[data-surface]').dataset.surface, kind, dotted: el.dataset.dot === 'true',
                            ratio: (Math.max(a, b) + .05) / (Math.min(a, b) + .05), foreground, background,
                            semantic, dot, expectedDot, label: rgba(style.color), expectedLabel: rgba(style.getPropertyValue('--fg').trim()) };
                    });
                }
                """);
            Assert.Equal(40, results.GetArrayLength());
            foreach (var result in results.EnumerateArray())
            {
                var ratio = result.GetProperty("ratio").GetDouble();
                Assert.True(ratio >= 4.5, $"{mode} {result.GetProperty("kind")} on {result.GetProperty("surface")}: {ratio:F2}:1");
                if (result.GetProperty("semantic").GetBoolean())
                    Assert.Equal(result.GetProperty("expectedLabel").GetRawText(), result.GetProperty("label").GetRawText());
                if (result.GetProperty("dotted").GetBoolean())
                    Assert.Equal(result.GetProperty("expectedDot").GetRawText(), result.GetProperty("dot").GetRawText());
            }
            await File.WriteAllTextAsync(Path.Combine(browser.Artifacts, $"contrast-{mode}.json"), results.GetRawText());
            await page.Locator("#badge-scene").ScreenshotAsync(new() { Path = Path.Combine(browser.Artifacts, $"visual-badges-{mode}.png") });
        }, ReducedMotion.Reduce);
}
