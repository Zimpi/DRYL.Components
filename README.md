# DRYL

[![NuGet](https://img.shields.io/nuget/v/DRYL.Components.svg)](https://www.nuget.org/packages/DRYL.Components)
[![Downloads](https://img.shields.io/nuget/dt/DRYL.Components.svg)](https://www.nuget.org/packages/DRYL.Components)
[![CI](https://github.com/Zimpi/DRYL.Components/actions/workflows/ci.yml/badge.svg)](https://github.com/Zimpi/DRYL.Components/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8%20%7C%209%20%7C%2010-512BD4.svg)](https://dotnet.microsoft.com/)
[![Website](https://img.shields.io/badge/docs-components.dryl.dev-7c3aed.svg)](https://components.dryl.dev/)
[![Sponsor](https://img.shields.io/badge/Sponsor-%E2%9D%A4-db61a2.svg?logo=github)](https://github.com/sponsors/Zimpi)

**Dark. Glassy. Alive — and AI-native.**
The open-source UI component library for **Blazor Server** and **Blazor WebAssembly**, built for products that ship with a model in the loop.

```bash
dotnet add package DRYL.Components
```

![DRYL — Mission Control, the sample app's overview page](docs/screenshots/overview.png)

<sub>One screen, ~30 components, nothing but a layout grid on top — the <code>/overview</code> page of the sample app. Everything in this picture, including the app shell it runs in, is a DRYL component.</sub>

> **Status: `1.0.0`** — the first stable release. The public API is frozen: any rename of a public parameter, event, enum or slot on an existing component is now a breaking change (MAJOR bump). Found a problem? [Open an issue](https://github.com/Zimpi/DRYL.Components/issues).

---

## Why DRYL?

Most Blazor component libraries are ports of Bootstrap or Material — safe, neutral, indistinguishable. DRYL starts from a different premise: **your app has a language model in it**, and AI is a first-class state of the UI, not a spinner you bolt on at the end.

- **AI-native.** Every AI-capable surface accepts a single `Ai` parameter that drives a shared visual vocabulary — rotating gradient border, streaming glow, one-shot reveal — across the whole library.
- **Dark only.** Translucent glass layers stacked on pure black. No light theme, no toggle — dark *is* the design.
- **Accents glow, never scream.** A violet-to-cyan gradient lives in 1px borders, glow rings and tiny indicators — never as a background fill.
- **Motion is intentional.** Three durations, three easings, system-wide. Nothing flickers, nothing crawls. Every component animates its enter, exit and state changes.
- **One token file.** Every color, spacing, radius, shadow and duration is a CSS variable in [`dryl.css`](DRYL.Components/wwwroot/dryl.css).
- **~90 components, zero JavaScript dependencies.** No npm, no JS framework underneath — just CSS, Razor, and minimal interop.
- **Accessible by default.** Keyboard-reachable, ARIA-labeled, visible focus rings — and AI activity announced via `aria-live`.

---

## Make it yours — theming

Most libraries make you hand-tune dozens of colors and hope they stay coherent.
DRYL flips that: you set a few **seeds** and the system **derives** the rest —
gradient, soft fills, accent lines, glow rings and the AI aura all stay in sync.

```razor
@* One line in your root layout *@
<DrylThemeProvider Theme="DrylThemes.Ember" />
```

Switch at runtime — the whole accent chain *glides* (and respects
`prefers-reduced-motion`):

```csharp
@inject IDrylThemeService Theme

await Theme.SetAccentAsync("#f59e0b", "#f43f5e"); // or Theme.SetThemeAsync(DrylThemes.Verdant)
```

Ships with curated presets — **Nebula** (default), **Ember**, **Verdant**,
**Mono** — and a dedicated, opt-in **AI accent** so AI moments can glow in their
own color. The dark glass core stays fixed by design, so a theme can't break the
look. Full guide: [`THEMING.md`](THEMING.md).

---

## Quick start

DRYL targets **.NET 8, 9 and 10**.

**1. Add the package**

```bash
dotnet add package DRYL.Components
```

**2. Register services** in `Program.cs`

```csharp
builder.Services.AddDrylComponents();
```

**3. Reference the stylesheet** in your host page (`App.razor` / `_Host.cshtml` / `wwwroot/index.html`)

```html
<link rel="stylesheet" href="_content/DRYL.Components/dryl.css" />
```

**4. Add the providers** once in your root layout

```razor
<DrylThemeProvider />
<DrylDialogProvider />
<DrylToastProvider />
```

**5. Use components**

```razor
@using DRYL.Components

<DrylCard>
    <DrylButton Variant="ButtonVariant.Primary">Hello DRYL</DrylButton>
</DrylCard>
```

---

## AI Mode — first-class citizen

Every surface that can carry AI-generated content accepts a single `Ai` parameter of type `AiState`. That parameter drives a consistent, learnable visual vocabulary across cards, tables, dialogs, inputs and more — users see the same rotating gradient border on a card that's streaming tokens as they do on a step being filled by a tool call.

### The five states

| State        | Visual                                                          | When to use                                                |
| ------------ | --------------------------------------------------------------- | ---------------------------------------------------------- |
| `None`       | Default styling — no AI signal.                                 | Surface is rendered normally, unrelated to AI output.      |
| `Active`     | Slow rotating gradient border + breathing accent glow.          | Persistent AI-driven surface (a chat panel, an LLM card).  |
| `Thinking`   | Faster pulse on border and glow.                                | A tool call is in flight.                                  |
| `Streaming`  | Moderate pulse; content updates incrementally.                  | Tokens are arriving from the model.                        |
| `Generated`  | One-shot accent wash sweep + soft lift.                         | Reveal moment immediately after generation completes.       |

### Wiring with `Microsoft.Extensions.AI`

```csharp
private AiState _state = AiState.None;

private async Task AskAi()
{
    _state = AiState.Thinking;
    var response = await chatClient.GetStreamingResponseAsync(prompt);

    _state = AiState.Streaming;
    await foreach (var chunk in response)
    {
        _text += chunk.Text;
        StateHasChanged();
    }

    _state = AiState.Generated;   // one-shot wash
    await Task.Delay(900);
    _state = AiState.Active;      // settle back to idle AI mode
}
```

```razor
<DrylCard Ai="@_state">
    <DrylAiIndicator State="@_state" />
    @_text
</DrylCard>
```

The CSS primitives behind this (`.ai-aura`, `.ai-aura-ring`, `.ai-aura-glow`, `.ai-aura-wash`) live in [`dryl.css`](DRYL.Components/wwwroot/dryl.css) and can be applied to any element that isn't yet a DRYL component. The full list of AI-aware components — including `DrylDialog` for Human-in-the-Middle flows and `DrylTable` for streaming rows — is at [components.dryl.dev](https://components.dryl.dev/).

---

## Where to go deeper

DRYL ships **~90 components across 8 categories** — actions, surfaces,
navigation, data, inputs, layout, feedback, and a dedicated **Intelligence**
set for agent UIs (token streams, tool-call traces, RAG citations,
human-in-the-middle review).

The complete, interactive reference — every component, variant and AI state —
lives at **[components.dryl.dev](https://components.dryl.dev/)**.

For the design language and customization model:
[`DESIGN_TOKENS.md`](DESIGN_TOKENS.md) ·
[`THEMING.md`](THEMING.md) ·
[`COMPONENT_PATTERNS.md`](COMPONENT_PATTERNS.md).

> Customization is just getting started — theming is step one toward a fully tunable DRYL.

---

## Contributing

DRYL is a solo effort built in the open, and with the API now frozen for `1.0` it's a great time to get involved. If you want to help:

1. Read [`CLAUDE.md`](CLAUDE.md) — the contribution rules (they apply to humans too).
2. Open an issue before starting work on a new component.
3. Every PR must respect the token system. No invented colors, no arbitrary spacings.

---

## Support DRYL

DRYL is built and maintained in the open as a solo effort. If it saves you time
or you'd like to see it reach a stable 1.0 faster, you can support the work:

[![Sponsor](https://img.shields.io/badge/Sponsor-%E2%9D%A4-db61a2.svg?logo=github)](https://github.com/sponsors/Zimpi)

- **[GitHub Sponsors](https://github.com/sponsors/Zimpi)** — one-off or recurring.

Sponsorships are appreciated but never required: DRYL is MIT-licensed and will
always be free to use. Starring the repo and filing good issues helps just as
much.

---

## Credits

DRYL stands on the shoulders of these open-source projects:

- **[Lucide](https://lucide.dev)** — the icon set behind `DrylIcon`. ISC-licensed. Some Lucide icons themselves derive from [Feather Icons](https://feathericons.com) (MIT, Cole Bemis).
- **[Inter](https://rsms.me/inter/)** by Rasmus Andersson — primary UI typeface. SIL Open Font License.
- **[JetBrains Mono](https://www.jetbrains.com/mono/)** — monospace typeface used for code, IDs and timestamps. SIL Open Font License.

Full license texts for bundled third-party assets are in [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md).

---

## License

MIT — see [`LICENSE`](LICENSE). Use it, fork it, ship it.
