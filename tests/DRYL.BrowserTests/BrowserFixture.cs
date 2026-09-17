using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Playwright;

namespace DRYL.BrowserTests;

[CollectionDefinition("Browser", DisableParallelization = true)]
public sealed class BrowserCollection : ICollectionFixture<BrowserFixture>;

/// <summary>Owns the real Blazor host and a browser; each case gets a clean context.</summary>
public sealed class BrowserFixture : IAsyncLifetime
{
    private Process? _host;
    private bool _hostStarted;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly List<string> _hostLog = [];
    public string Root { get; } = FindRoot();
    public string BrowserName { get; } = Environment.GetEnvironmentVariable("DRYL_BROWSER") ?? "chromium";
    public string Url { get; private set; } = "";
    public string Artifacts => Path.Combine(Root, "artifacts", "browser", BrowserName);

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Artifacts);
        try
        {
            using (var port = new TcpListener(IPAddress.Loopback, 0))
            {
                port.Start();
                Url = $"http://127.0.0.1:{((IPEndPoint)port.LocalEndpoint).Port}";
            }
            var start = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = Root,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            foreach (var arg in new[] { "run", "--no-build", "--no-restore", "--project",
                "tests/DRYL.BrowserHost/DRYL.BrowserHost.csproj", "-c", "Release", "--no-launch-profile", "--urls", Url })
                start.ArgumentList.Add(arg);
            start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            start.Environment["DOTNET_ENVIRONMENT"] = "Development";
            _host = new Process { StartInfo = start };
            _host.OutputDataReceived += (_, e) => Log(e.Data);
            _host.ErrorDataReceived += (_, e) => Log(e.Data);
            _hostStarted = _host.Start();
            if (!_hostStarted) throw new InvalidOperationException("Browser host could not start.");
            _host.BeginOutputReadLine();
            _host.BeginErrorReadLine();

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var watch = Stopwatch.StartNew();
            while (true)
            {
                if (_host.HasExited) throw new InvalidOperationException($"Browser host exited: {ReadLog()}");
                try
                {
                    using var response = await client.GetAsync(Url);
                    if (response.IsSuccessStatusCode) break;
                }
                catch (HttpRequestException) { }
                catch (TaskCanceledException) { }
                if (watch.Elapsed > TimeSpan.FromSeconds(45))
                    throw new TimeoutException($"Browser host did not start: {ReadLog()}");
                await Task.Delay(100);
            }

            _playwright = await Playwright.CreateAsync();
            var engine = BrowserName switch
            {
                "chromium" or "chrome" or "msedge" => _playwright.Chromium,
                "firefox" => _playwright.Firefox,
                "webkit" => _playwright.Webkit,
                _ => throw new ArgumentException($"Unsupported DRYL_BROWSER: {BrowserName}"),
            };
            _browser = await engine.LaunchAsync(new()
            {
                Headless = true,
                Channel = BrowserName is "chrome" or "msedge" ? BrowserName : null,
            });
            await File.WriteAllTextAsync(Path.Combine(Artifacts, "environment.txt"),
                $"Browser: {BrowserName} {_browser.Version}\nOS: {Environment.OSVersion}\n" +
                $"Playwright: {typeof(IPlaywright).Assembly.GetName().Version}\nHost: {Url}\n");
        }
        catch
        {
            try { await DisposeAsync(); } catch { /* Preserve the startup failure. */ }
            throw;
        }
    }

    public async Task RunAsync(string name, string mode, Func<IPage, Task> test,
        ReducedMotion reducedMotion = ReducedMotion.NoPreference,
        ForcedColors forcedColors = ForcedColors.None)
    {
        if (_browser is null) throw new InvalidOperationException("Browser fixture did not initialize.");
        await using var context = await _browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 1000 },
            ColorScheme = mode == "light" ? ColorScheme.Light : ColorScheme.Dark,
            ReducedMotion = reducedMotion,
            ForcedColors = forcedColors,
        });
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(10000);
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        page.Console += (_, message) =>
        {
            if (message.Type == "error") errors.Add(message.Text);
        };
        var artifactName = string.Concat($"{name}-{mode}".Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '_'));
        try
        {
            await page.GotoAsync(Url);
            await Assertions.Expect(page.GetByTestId("host-ready")).ToBeVisibleAsync();
            await page.EvaluateAsync("mode => document.documentElement.setAttribute('data-dryl-mode', mode)", mode);
            await test(page);
            Assert.Empty(errors);
            await context.Tracing.StopAsync();
        }
        catch
        {
            // Artifacts should not replace the original failure when a browser has crashed.
            try { await page.ScreenshotAsync(new() { Path = Path.Combine(Artifacts, artifactName + ".png"), FullPage = true }); } catch { }
            try { await context.Tracing.StopAsync(new() { Path = Path.Combine(Artifacts, artifactName + ".zip") }); } catch { }
            try { await File.WriteAllLinesAsync(Path.Combine(Artifacts, artifactName + "-errors.txt"), errors); } catch { }
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        try { if (_browser is not null) await _browser.CloseAsync(); }
        finally
        {
            _browser = null;
            _playwright?.Dispose();
            _playwright = null;
            if (_host is not null)
            {
                if (_hostStarted && !_host.HasExited) { _host.Kill(entireProcessTree: true); await _host.WaitForExitAsync(); }
                _host.Dispose();
                _host = null;
                _hostStarted = false;
            }
            Directory.CreateDirectory(Artifacts);
            await File.WriteAllTextAsync(Path.Combine(Artifacts, "host.log"), ReadLog());
        }
    }

    private void Log(string? line) { if (line is not null) lock (_hostLog) _hostLog.Add(line); }
    private string ReadLog() { lock (_hostLog) return string.Join(Environment.NewLine, _hostLog); }
    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "DRYL.slnx"))) return dir.FullName;
        throw new DirectoryNotFoundException("Run browser tests from a DRYL.Components checkout.");
    }
}
