# DRYL Components

[![GitHub Stars](https://img.shields.io/github/stars/Zimpi/DRYL.Components?style=flat&color=7c3aed&label=stars)](https://github.com/Zimpi/DRYL.Components/stargazers)
[![NuGet](https://img.shields.io/nuget/v/DRYL.Components.svg?color=512BD4)](https://www.nuget.org/packages/DRYL.Components)
[![Downloads](https://img.shields.io/nuget/dt/DRYL.Components.svg)](https://www.nuget.org/packages/DRYL.Components)
[![CI](https://github.com/Zimpi/DRYL.Components/actions/workflows/ci.yml/badge.svg)](https://github.com/Zimpi/DRYL.Components/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8%20%7C%209%20%7C%2010-512BD4.svg)](https://dotnet.microsoft.com/)
[![Sponsor](https://img.shields.io/badge/Sponsor-%E2%9D%A4-db61a2.svg?logo=github)](https://github.com/sponsors/Zimpi)

**An AI-Native, Zero-NPM, Glassmorphic UI Component Library for Blazor.**
**Built for the era of LLMs and high-performance SaaS applications.**

[![Live Demo](https://img.shields.io/badge/→_Live_Demo-components.dryl.dev-7c3aed?style=for-the-badge)](https://components.dryl.dev/)

</div>


| Aurora Glow Cards | Live Token Streaming | Agentic Tool Calls |
| :---: | :---: | :---: |
| ![DRYL — Cards Aurora Glow](https://raw.githubusercontent.com/Zimpi/DRYL.Components/main/docs/gifs/drylcards.gif) | ![DRYL — Chat Streaming](https://raw.githubusercontent.com/Zimpi/DRYL.Components/main/docs/gifs/drylchat.gif) | ![DRYL — Function Call](https://raw.githubusercontent.com/Zimpi/DRYL.Components/main/docs/gifs/drylfunctioncall.gif) |
| Responsive card layouts with AI-driven gradient borders and breathing glow effects | Real-time token streaming with animated content updates and live AI indicators | Tool-call chains with animated state transitions and contextual feedback |

---

## Why DRYL?

Most Blazor component libraries are ports of Bootstrap or Material — safe, neutral, indistinguishable. DRYL starts from a different premise: **your app has a language model in it**, and AI is a first-class state of the UI, not a spinner you bolt on at the end.

- **AI-native.** Every AI-capable surface accepts a single `Ai` parameter that drives a shared visual vocabulary — rotating gradient border, streaming glow, one-shot reveal — across the whole library.
- **Light & dark, one identity.** Translucent glass layers on a deep-dark or luminous-light ground — following the user's system by default, switchable and persisted at runtime.
- **Accents glow, never scream.** A violet-to-cyan gradient lives in 1px borders, glow rings and tiny indicators — never as a background fill.
- **Motion is intentional.** Three durations, three easings, system-wide. Nothing flickers, nothing crawls. Every component animates its enter, exit and state changes.
- **One token file.** Every color, spacing, radius, shadow and duration is a CSS variable in [`dryl.css`](https://github.com/Zimpi/DRYL.Components/blob/main/code/DRYL.Components/wwwroot/dryl.css).
- **~90 components, zero npm dependencies.** No JS framework underneath, no third-party package — just CSS, Razor, and a single hand-written interop file ([`dryl.js`](https://github.com/Zimpi/DRYL.Components/blob/main/code/DRYL.Components/wwwroot/js/dryl.js)) for the DOM-level concerns Blazor can't do alone (focus traps, portals, clipboard).
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

**3. Reference the assets** in your host page (`App.razor` / `_Host.cshtml` / `wwwroot/index.html`)

```html
<head>
    <!-- … -->
    <link rel="stylesheet" href="_content/DRYL.Components/dryl.css" />
</head>
<body>
    <!-- … -->
    <script src="_content/DRYL.Components/js/dryl.js"></script>
</body>
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

> Found a problem? [Open an issue](https://github.com/Zimpi/DRYL.Components/issues).

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