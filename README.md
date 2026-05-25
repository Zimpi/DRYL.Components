# DRYL

An open-source UI component library for **Blazor Server** and **Blazor WebAssembly** with an unapologetically modern, dark aesthetic.

> **Status: Work in progress — not production-ready.**
> DRYL is being built in the open. The design system is in place and the first reference components exist, but the library is **not yet suitable for production use**. Expect breaking changes, missing components, and rough edges until `1.0`.

---

## Vision

DRYL is **dark, glassy, alive**.

Most Blazor component libraries feel like ports of Bootstrap or Material — safe, neutral, indistinguishable. DRYL is the opposite. Surfaces are translucent layers stacked on pure black, accents glow in a violet-to-cyan gradient, and motion is intentional rather than decorative.

The goal is a small, opinionated set of components — buttons, cards, inputs, tables, modals, navigation — that look like they belong in a product built in 2026, not 2014. Every component reads from a single token file ([`dryl.css`](DRYL.Components/wwwroot/dryl.css)), so the entire visual language can be re-tuned in one place.

**Principles**

- **Token-driven.** Every color, spacing, radius, shadow and duration is a CSS variable. No magic numbers.
- **Dark only.** No light theme. Dark is the design, not a toggle.
- **Glass surfaces.** Translucent layers with `backdrop-filter`, never solid blocks.
- **No JS frameworks.** Zero npm packages on top of Blazor — just CSS, Razor, and minimal interop.
- **Accessible by default.** Keyboard-reachable, ARIA-labeled, visible focus rings.

---

## Preview

> Screenshots will follow as components land. The placeholders below point to where they'll live.

### Design system overview
<!-- screenshot: full prototype overview -->
![DRYL — design system overview](docs/screenshots/overview.png)

### Buttons
<!-- screenshot: DrylButton variants (Primary / Secondary / Ghost / Danger) + sizes + loading -->
![DrylButton — variants and states](docs/screenshots/buttons.png)

### Cards
<!-- screenshot: DrylCard with cursor spotlight -->
![DrylCard — glass surface with cursor spotlight](docs/screenshots/cards.png)

### Badges
<!-- screenshot: DrylBadge kinds (Neutral / Accent / Success / Warning / Danger) -->
![DrylBadge — status pills](docs/screenshots/badges.png)

---

## What's in the box (today)

| Component       | Status     | Notes                                              |
| --------------- | ---------- | -------------------------------------------------- |
| `DrylButton`    | Reference  | Primary / Secondary / Ghost / Danger, sizes, loading, icon slots |
| `DrylCard`      | Reference  | Glass surface with optional cursor-tracking spotlight |
| `DrylBadge`     | Reference  | Neutral / Accent / Success / Warning / Danger, optional dot |
| `DrylIcon`      | Planned    | Referenced by Button & Badge — next on the list    |
| `DrylInputText` | Planned    | Form-bound input with leading/trailing icons       |
| `DrylTable`     | Planned    | Data grid with sticky header, sortable columns     |
| `DrylModal`     | Planned    | Glass overlay with focus trap                      |
| `DrylToast`     | Planned    | Programmatic notifications via service             |

For the full design language, see [`DESIGN_TOKENS.md`](DESIGN_TOKENS.md) and [`COMPONENT_PATTERNS.md`](COMPONENT_PATTERNS.md).

---

## Repository layout

```
DRYL.Components/             The library (Razor Class Library, .NET 10)
  Components/
    Actions/                 DrylButton, ...
    Surfaces/                DrylCard, ...
    Data/                    DrylBadge, ...
  wwwroot/
    dryl.css                 The single stylesheet — every token, every primitive
    js/dryl.js               Minimal JS interop (namespaced as window.dryl.*)

samples/DRYL.Components.Demo/   Sample Blazor app showing the components live
prototype/                       Original HTML/JSX prototype — visual target
CLAUDE.md                        Rules for AI agents contributing to DRYL
DESIGN_TOKENS.md                 Token reference
COMPONENT_PATTERNS.md            Component anatomy & folder conventions
SETUP.md                         Quick-start guide for new consumers
```

---

## Try it locally

DRYL is not yet published to NuGet. To explore the demo app:

```bash
git clone https://github.com/<your-handle>/DRYL.Components.git
cd DRYL.Components
dotnet run --project samples/DRYL.Components.Demo
```

---

## Contributing

Right now this is a solo effort, but contributions will be welcome once the core stabilizes. If you want to help:

1. Read [`CLAUDE.md`](CLAUDE.md) — the contribution rules (they apply to humans too).
2. Open an issue before starting work on a new component.
3. Every PR must respect the token system. No invented colors, no arbitrary spacings.

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
