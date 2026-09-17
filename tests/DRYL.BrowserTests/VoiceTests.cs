using Microsoft.Playwright;

namespace DRYL.BrowserTests;

[Collection("Browser")]
public sealed class VoiceTests(BrowserFixture browser)
{
    public static TheoryData<string, string> StartupBoundaries
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var mode in new[] { "dark", "light" })
            foreach (var stage in new[] { "media", "offer", "local-description", "fetch", "answer", "remote-description" })
                data.Add(mode, stage);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(StartupBoundaries))]
    public Task Stop_and_restart_before_old_startup_settles(string mode, string stage) =>
        browser.RunAsync($"{nameof(Stop_and_restart_before_old_startup_settles)}-{stage}", mode, async page =>
        {
            await HoldOnly(page, stage);
            await page.Locator("#voice-start").ClickAsync();
            var oldId = await Pending(page, stage);
            await page.Locator("#voice-stop").ClickAsync();
            await Phase(page, "Idle");
            await NoResources(page);

            await page.Locator("#voice-start").ClickAsync();
            var newId = await Pending(page, stage, oldId);
            await Settle(page, stage, newId);
            await Phase(page, "Live");
            await Settle(page, stage, oldId);
            await Roundtrip(page);
            await Phase(page, "Live");
            await Assertions.Expect(page.Locator("#voice-error")).ToBeEmptyAsync();
            Assert.Equal(1, await page.EvaluateAsync<int>("() => voiceFixture.stats().liveTracks"));
            Assert.Equal(1, await page.EvaluateAsync<int>("() => voiceFixture.stats().livePeers"));

            await page.Locator("#voice-stop").ClickAsync();
            await Phase(page, "Idle");
            await NoResources(page);
        });

    [Theory]
    [InlineData("dark", false)]
    [InlineData("light", false)]
    [InlineData("dark", true)]
    [InlineData("light", true)]
    public Task Disposed_or_rejected_permission_cannot_damage_replacement(string mode, bool reject) =>
        browser.RunAsync($"{nameof(Disposed_or_rejected_permission_cannot_damage_replacement)}-{reject}", mode, async page =>
        {
            await page.Locator("#voice-start").ClickAsync();
            var oldId = await Pending(page, "media");
            await page.Locator("#voice-dispose").ClickAsync();
            await Assertions.Expect(page.Locator("#voice-disposed")).ToHaveTextAsync("true");
            await Phase(page, "Idle");
            await page.Locator("#voice-recreate").ClickAsync();
            await page.Locator("#voice-start").ClickAsync();
            var newId = await Pending(page, "media", oldId);
            await Settle(page, "media", newId);
            await Phase(page, "Live");
            await Settle(page, "media", oldId, reject);
            await Roundtrip(page);
            await Phase(page, "Live");
            await Assertions.Expect(page.Locator("#voice-error")).ToBeEmptyAsync();
            Assert.Equal(1, await page.EvaluateAsync<int>("() => voiceFixture.stats().liveTracks"));
            await page.Locator("#voice-stop").ClickAsync();
            await NoResources(page);
        });

    [Theory]
    [InlineData("dark")]
    [InlineData("light")]
    public Task Real_dock_can_stop_connecting_and_remount_a_live_run(string mode) =>
        browser.RunAsync(nameof(Real_dock_can_stop_connecting_and_remount_a_live_run), mode, async page =>
        {
            await page.Locator("#voice-dock-toggle").ClickAsync();
            var microphone = page.GetByRole(AriaRole.Button, new() { Name = "Start fixture voice", Exact = true });
            await microphone.FocusAsync();
            await page.Keyboard.PressAsync("Enter");
            var oldId = await Pending(page, "media");
            await Phase(page, "Connecting");
            var stop = page.Locator("#voice-dock .dock-voice-stop");
            await stop.FocusAsync();
            await page.Keyboard.PressAsync("Enter");
            await Phase(page, "Idle");
            await Settle(page, "media", oldId);
            await NoResources(page);

            await microphone.ClickAsync();
            await Settle(page, "media", await Pending(page, "media"));
            await Phase(page, "Live");
            await page.EvaluateAsync("() => voiceFixture.emit({ type: 'conversation.item.input_audio_transcription.completed', transcript: 'Current conversation' })");
            await Assertions.Expect(page.Locator("#voice-transcript")).ToHaveTextAsync("1");
            await Assertions.Expect(page.Locator("#voice-dock .dock-voice-line")).ToHaveTextAsync("Current conversation");
            await page.Locator("#voice-dock-toggle").ClickAsync();
            await Assertions.Expect(page.Locator("#voice-dock")).ToHaveCountAsync(0);
            await Phase(page, "Live");
            Assert.Equal(1, await page.EvaluateAsync<int>("() => voiceFixture.stats().liveTracks"));
            await page.Locator("#voice-dock-toggle").ClickAsync();
            await Assertions.Expect(page.Locator("#voice-dock .voice-orb")).ToBeVisibleAsync();
            await stop.ClickAsync();
            await Phase(page, "Idle");
            await NoResources(page);
        });

    private static async Task HoldOnly(IPage page, string stage) =>
        await page.EvaluateAsync("stage => { voiceFixture.hold('media', false); voiceFixture.hold(stage); }", stage);

    private static async Task<int> Pending(IPage page, string stage, int except = -1)
    {
        await page.WaitForFunctionAsync("arg => voiceFixture.pending().some(p => p.stage === arg.stage && p.id !== arg.except)", new { stage, except });
        return await page.EvaluateAsync<int>("arg => voiceFixture.pending().find(p => p.stage === arg.stage && p.id !== arg.except).id", new { stage, except });
    }

    private static async Task Settle(IPage page, string stage, int id, bool reject = false) =>
        await page.EvaluateAsync("arg => voiceFixture[arg.reject ? 'reject' : 'complete'](arg.stage, arg.id)", new { stage, id, reject });

    private static Task Phase(IPage page, string expected) =>
        Assertions.Expect(page.Locator("#voice-phase")).ToHaveTextAsync(expected);

    private static async Task NoResources(IPage page) =>
        await page.WaitForFunctionAsync("() => { const s = voiceFixture.stats(); return ['liveTracks', 'livePeers', 'liveChannels', 'liveContexts', 'audioElements', 'timers', 'frames'].every(key => s[key] === 0); }");

    private static async Task Roundtrip(IPage page)
    {
        // The .NET callback and this event share the circuit; observe its next render.
        await page.GetByLabel("Name", new() { Exact = true }).FillAsync("voice-check");
        await page.GetByLabel("Name", new() { Exact = true }).PressAsync("Tab");
        await Assertions.Expect(page.Locator("#focus-values")).ToContainTextAsync("voice-check");
    }
}
