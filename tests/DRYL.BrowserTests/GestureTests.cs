using Microsoft.Playwright;

namespace DRYL.BrowserTests;

[Collection("Browser")]
public sealed class GestureTests(BrowserFixture browser)
{
    public static TheoryData<string, string, string> TableCancellations
    {
        get
        {
            var data = new TheoryData<string, string, string>();
            foreach (var mode in new[] { "dark", "light" })
            foreach (var key in new[] { "name", "status" })
            foreach (var cancel in new[] { "Escape", "pointercancel", "remove" })
                data.Add(mode, key, cancel);
            return data;
        }
    }

    public static TheoryData<string, string> CanvasCancellations
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var mode in new[] { "dark", "light" })
            foreach (var cancel in new[] { "Escape", "pointercancel", "remove" })
                data.Add(mode, cancel);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(TableCancellations))]
    public Task Table_cancel_restores_declared_and_unset_widths(string mode, string key, string cancel) =>
        browser.RunAsync($"{nameof(Table_cancel_restores_declared_and_unset_widths)}-{key}-{cancel}", mode, async page =>
        {
            await PrepareTable(page, key);
            await StartTableDrag(page, key);
            await ForeignPointerCannotChangePreview(page);
            await MoveTableGrip(page);
            Assert.True(await page.EvaluateAsync<bool>("() => gestureProbe.widths() !== gestureProbe.before"));
            await Cancel(page, "table", cancel);
            await AssertClean(page);
            Assert.True(await page.EvaluateAsync<bool>("() => gestureProbe.widths() === gestureProbe.before"));
            await ReplayLateEvents(page);
            await Roundtrip(page);
            Assert.True(await page.EvaluateAsync<bool>("() => gestureProbe.widths() === gestureProbe.before"));
            Assert.Equal(0, await page.EvaluateAsync<int>("() => gestureProbe.saves"));
            Assert.Null(await page.EvaluateAsync<string?>("() => localStorage.getItem('i13-table-fixture')"));
            if (cancel == "remove")
            {
                await page.Locator("#table-toggle").ClickAsync();
                await Assertions.Expect(page.Locator("#table-scene table")).ToBeVisibleAsync();
                Assert.Equal(key == "name" ? "180px" : "", await page.Locator($"#table-scene th[data-col-key='{key}']")
                    .EvaluateAsync<string>("el => el.style.width"));
            }
        });

    [Theory]
    [InlineData("dark", "name")]
    [InlineData("light", "name")]
    [InlineData("dark", "status")]
    [InlineData("light", "status")]
    public Task Table_release_commits_once_and_survives_remount(string mode, string key) =>
        browser.RunAsync($"{nameof(Table_release_commits_once_and_survives_remount)}-{key}", mode, async page =>
        {
            await PrepareTable(page, key);
            await StartTableDrag(page, key);
            await ForeignPointerCannotChangePreview(page);
            await MoveTableGrip(page);
            var committed = await page.EvaluateAsync<string>("() => gestureProbe.cells[0].style.width");
            await page.Mouse.UpAsync();
            await AssertClean(page);
            await page.WaitForFunctionAsync("() => gestureProbe.saves === 1");
            await ReplayLateEvents(page);
            await Roundtrip(page);
            Assert.Equal(1, await page.EvaluateAsync<int>("() => gestureProbe.saves"));
            Assert.Equal(committed, await page.EvaluateAsync<string>(
                "key => JSON.parse(localStorage.getItem('i13-table-fixture')).Widths[key]", key));
            await page.Locator("#table-toggle").ClickAsync();
            await Assertions.Expect(page.Locator("#table-scene table")).ToHaveCountAsync(0);
            await page.Locator("#table-toggle").ClickAsync();
            await page.WaitForFunctionAsync("([key, width]) => document.querySelector(`#table-scene th[data-col-key='${key}']`)?.style.width === width",
                new[] { key, committed });
        });

    [Theory]
    [MemberData(nameof(CanvasCancellations))]
    public Task Canvas_cancel_removes_drag_decoration_and_preserves_order(string mode, string cancel) =>
        browser.RunAsync($"{nameof(Canvas_cancel_removes_drag_decoration_and_preserves_order)}-{cancel}", mode, async page =>
        {
            await PrepareCanvas(page);
            await StartCanvasDrag(page);
            await ForeignPointerCannotChangePreview(page);
            await MoveCanvasGrip(page);
            Assert.True(await page.EvaluateAsync<bool>("() => !!gestureProbe.root.querySelector('[data-drop-after]')"));
            await Cancel(page, "canvas", cancel);
            await AssertClean(page);
            await AssertCanvasDecorationRestored(page);
            await ReplayLateEvents(page);
            await Roundtrip(page);
            await AssertCanvasDecorationRestored(page);
            await Assertions.Expect(page.Locator("#canvas-edits")).ToHaveTextAsync("0");
            await Assertions.Expect(page.Locator("#canvas-order")).ToHaveTextAsync("first,second,third");
            if (cancel == "remove")
            {
                await page.Locator("#canvas-toggle").ClickAsync();
                await Assertions.Expect(page.Locator("#canvas-scene [data-cid='first']")).ToBeVisibleAsync();
                await Assertions.Expect(page.Locator("#canvas-scene .is-dragging")).ToHaveCountAsync(0);
            }
        });

    [Theory]
    [InlineData("dark")]
    [InlineData("light")]
    public Task Canvas_changed_drop_commits_once(string mode) =>
        browser.RunAsync(nameof(Canvas_changed_drop_commits_once), mode, async page =>
        {
            await PrepareCanvas(page);
            await StartCanvasDrag(page);
            await ForeignPointerCannotChangePreview(page);
            await MoveCanvasGrip(page);
            await page.Mouse.UpAsync();
            await AssertClean(page);
            await Assertions.Expect(page.Locator("#canvas-edits")).ToHaveTextAsync("1");
            await Assertions.Expect(page.Locator("#canvas-order")).ToHaveTextAsync("second,third,first");
            await ReplayLateEvents(page);
            await Roundtrip(page);
            await Assertions.Expect(page.Locator("#canvas-edits")).ToHaveTextAsync("1");
            await Assertions.Expect(page.Locator("#canvas-order")).ToHaveTextAsync("second,third,first");
            await Assertions.Expect(page.Locator("#canvas-scene .is-dragging")).ToHaveCountAsync(0);
            await Assertions.Expect(page.Locator("#canvas-scene [data-drop-before], #canvas-scene [data-drop-after]")).ToHaveCountAsync(0);
        });

    private static async Task InstallProbe(IPage page)
    {
        // Observe the actual gesture handlers and persistence call without replacing
        // the production .NET target, pointer loop, DOM nodes or browser layout.
        await page.EvaluateAsync("""
            () => {
                const types = new Set(['pointermove', 'pointerup', 'pointercancel', 'keydown']);
                const active = new Map();
                const history = [];
                const add = window.addEventListener.bind(window);
                const remove = window.removeEventListener.bind(window);
                window.addEventListener = (type, handler, options) => {
                    if (types.has(type)) {
                        if (!active.has(type)) active.set(type, new Set());
                        active.get(type).add(handler);
                        history.push({ type, handler });
                    }
                    add(type, handler, options);
                };
                window.removeEventListener = (type, handler, options) => {
                    active.get(type)?.delete(handler);
                    remove(type, handler, options);
                };
                const probe = window.gestureProbe = {
                    count: () => [...active.values()].reduce((sum, handlers) => sum + handlers.size, 0),
                    history, saves: 0
                };
                const set = window.dryl.storage.set;
                window.dryl.storage.set = (key, value) => {
                    if (key === 'i13-table-fixture') probe.saves++;
                    return set(key, value);
                };
            }
            """);
    }

    private static async Task PrepareTable(IPage page, string key)
    {
        await page.WaitForFunctionAsync("() => document.querySelector('#table-scene .tbl-root')?.__drylResizeAttached === true");
        await InstallProbe(page);
        await page.EvaluateAsync("""
            key => {
                const p = gestureProbe;
                p.root = document.querySelector('#table-scene .tbl-root');
                p.grip = p.root.querySelector(`.tbl-col-resize[data-col-key='${key}']`);
                p.cells = [...p.root.querySelectorAll(`th[data-col-key='${key}'], td[data-col-key='${key}']`)];
                p.widths = () => JSON.stringify(p.cells.map(cell => [cell.style.width, cell.style.getPropertyPriority('width')]));
                p.before = p.widths();
                p.snapshot = p.widths;
                p.grip.addEventListener('pointerdown', e => p.pointerId = e.pointerId, { once: true });
            }
            """, key);
    }

    private static async Task PrepareCanvas(IPage page)
    {
        await page.Locator("#canvas-scene [data-cid='first']").ClickAsync();
        await Assertions.Expect(page.Locator("#canvas-scene [data-cid='first'] [data-drag-handle]")).ToBeVisibleAsync();
        // Keep the destination inside the viewport too. Firefox does not deliver
        // a native pointermove to a target below the viewport's lower edge.
        await page.Locator("#canvas-scene").ScrollIntoViewIfNeededAsync();
        await page.Locator("#canvas-scene [data-cid='first']").EvaluateAsync("async el => await Promise.all(el.getAnimations({ subtree: true }).filter(a => a.effect?.getTiming().iterations !== Infinity).map(a => a.finished.catch(() => {})))");
        await InstallProbe(page);
        await page.EvaluateAsync("""
            () => {
                const p = gestureProbe;
                p.root = document.querySelector('#canvas-scene .canvas-body');
                p.node = p.root.querySelector('[data-cid="first"]');
                p.grip = p.node.querySelector('[data-drag-handle]');
                p.before = p.node.style.transform;
                p.snapshot = () => p.node.style.transform;
                p.grip.addEventListener('pointerdown', e => p.pointerId = e.pointerId, { once: true });
            }
            """);
    }

    private static async Task StartTableDrag(IPage page, string key) =>
        await StartDrag(page, page.Locator($"#table-scene .tbl-col-resize[data-col-key='{key}']"));

    private static async Task StartCanvasDrag(IPage page) =>
        await StartDrag(page, page.Locator("#canvas-scene [data-cid='first'] [data-drag-handle]"));

    private static async Task StartDrag(IPage page, ILocator grip)
    {
        await grip.ScrollIntoViewIfNeededAsync();
        // Existing Canvas toolbar layering lets markdown cover the grip's
        // center. Use a genuinely exposed point for the ownership regression;
        // no force option, synthetic pointerdown or stylesheet override.
        var point = await grip.EvaluateAsync<float[]?>("""
            el => {
                const rect = el.getBoundingClientRect();
                for (const fy of [.5, .1, .9]) for (const fx of [.5, .1, .9]) {
                    const x = rect.x + rect.width * fx, y = rect.y + rect.height * fy;
                    if (el.contains(document.elementFromPoint(x, y))) return [x, y];
                }
                return null;
            }
            """) ?? throw new InvalidOperationException("No exposed pointer target exists on the actual gesture grip.");
        await page.Mouse.MoveAsync(point[0], point[1]);
        await page.EvaluateAsync("([x, y]) => { gestureProbe.x = x; gestureProbe.y = y; }",
            point.Select(value => (double)value).ToArray());
        await page.Mouse.DownAsync();
        Assert.True(await page.EvaluateAsync<bool>("() => gestureProbe.grip.hasPointerCapture(gestureProbe.pointerId)"));
        Assert.True(await page.EvaluateAsync<int>("() => gestureProbe.count()") == 4,
            await page.EvaluateAsync<string>("() => JSON.stringify(gestureProbe.history.map(({type,handler}) => ({type, source: handler.toString()})))"));
    }

    private static async Task MoveTableGrip(IPage page)
    {
        var point = await page.EvaluateAsync<float[]>("() => [gestureProbe.x + 64, gestureProbe.y]");
        await page.Mouse.MoveAsync(point[0], point[1]);
    }

    private static async Task MoveCanvasGrip(IPage page)
    {
        var rect = await page.Locator("#canvas-scene [data-cid='third']").BoundingBoxAsync()
            ?? throw new InvalidOperationException("Drop target has no layout box.");
        await page.Mouse.MoveAsync(rect.X + rect.Width / 2, rect.Y + rect.Height - 4);
        await page.WaitForFunctionAsync("() => !!gestureProbe.root.querySelector('[data-drop-after]')");
    }

    private static async Task ForeignPointerCannotChangePreview(IPage page)
    {
        Assert.True(await page.EvaluateAsync<bool>("""
            () => {
                const p = gestureProbe;
                const before = p.snapshot();
                for (const type of ['pointermove', 'pointerup', 'pointercancel'])
                    window.dispatchEvent(new PointerEvent(type, { pointerId: p.pointerId + 100, clientX: 1, clientY: 1 }));
                return p.snapshot() === before && p.grip.hasPointerCapture(p.pointerId) && p.count() === 4;
            }
            """));
    }

    private static async Task Cancel(IPage page, string family, string reason)
    {
        if (reason == "Escape") await page.Keyboard.PressAsync("Escape");
        else if (reason == "pointercancel")
            await page.EvaluateAsync("() => window.dispatchEvent(new PointerEvent('pointercancel', { pointerId: gestureProbe.pointerId }))");
        else
        {
            // A click evaluation requests a real Blazor removal without releasing
            // the held mouse first, which would otherwise commit the gesture.
            await page.Locator($"#{family}-toggle").EvaluateAsync("el => el.click()");
            await page.WaitForFunctionAsync("() => !gestureProbe.root.isConnected");
        }
        await page.Mouse.UpAsync();
    }

    private static async Task AssertClean(IPage page)
    {
        await page.WaitForFunctionAsync("() => gestureProbe.count() === 0");
        Assert.False(await page.EvaluateAsync<bool>("() => gestureProbe.grip.hasPointerCapture(gestureProbe.pointerId)"));
        Assert.False(await page.EvaluateAsync<bool>("() => gestureProbe.root.classList.contains('tbl-resizing')"));
    }

    private static async Task AssertCanvasDecorationRestored(IPage page)
    {
        Assert.True(await page.EvaluateAsync<bool>("""
            () => gestureProbe.node.style.transform === gestureProbe.before &&
                !gestureProbe.node.classList.contains('is-dragging') &&
                !gestureProbe.root.querySelector('[data-drop-before], [data-drop-after]')
            """));
    }

    private static async Task ReplayLateEvents(IPage page) => await page.EvaluateAsync("""
        () => {
            const p = gestureProbe;
            for (const { type, handler } of p.history)
                if (type === 'pointermove' || type === 'pointerup')
                    handler(new PointerEvent(type, { pointerId: p.pointerId, clientX: 999, clientY: 999 }));
        }
        """);

    private static async Task Roundtrip(IPage page)
    {
        await page.GetByLabel("Name", new() { Exact = true }).FillAsync("gesture-check");
        await Assertions.Expect(page.Locator("#focus-values")).ToContainTextAsync("gesture-check");
    }
}
