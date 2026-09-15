# I13 browser regressions

These development-only projects are intentionally outside `DRYL.slnx`. Ordinary
solution tests require no browser installation. The test fixture starts a real
.NET 10 interactive Blazor Server host against both working-tree projects and
loads the global stylesheet, JavaScript and generated CSS-isolation bundle.

```powershell
dotnet build tests/DRYL.BrowserTests/DRYL.BrowserTests.csproj -c Release
pwsh tests/DRYL.BrowserTests/bin/Release/net10.0/playwright.ps1 install chromium firefox webkit
$env:DRYL_BROWSER = 'chromium' # repeat with firefox and webkit
dotnet test tests/DRYL.BrowserTests/DRYL.BrowserTests.csproj -c Release --no-build
```

On Linux, install browser system dependencies too (`install --with-deps`). The
Playwright package fixes its matching browser revisions. Missing dependencies
fail the suite; cases are not silently skipped. `DRYL_BROWSER=chrome` or `msedge`
uses an already-installed branded browser without replacing the user's install.

Each test gets a fresh browser context. Failure screenshots and Playwright traces,
browser/OS versions and host logs go to `artifacts/browser/<engine>/`. CI uploads
these with test results. Traces contain only fixture content. Voice uses fake
media, WebRTC and network responses, never a physical microphone or provider key.

These are live behaviour tests, not approved appearance baselines for I8. Native
Safari/Firefox checks, physical devices and the Windows contrast-theme pass must
be recorded separately from Playwright emulation. Final evidence and outstanding
limits are tracked in `docs/2026-09-15-i13-implementation-plan.md`.
