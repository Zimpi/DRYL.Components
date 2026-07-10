# Instructions for Claude (and any AI agent) — DRYL Component Library

You are helping build **DRYL**, an open-source UI component library for Blazor Server and Blazor WebAssembly. Your job is to produce new components that are **visually consistent**, **token-driven**, and **idiomatic Blazor**.

Read this file before doing any work. Read it again if you find yourself inventing a color, a spacing value, or a one-off animation.

---

## 1. The system in one paragraph

DRYL is **glassy, alive — and AI-native**, rendered in two color modes that are one identity: translucent layers stacked on a deep-dark or luminous-light ground, following the user's system by default. Accents glow (violet → cyan gradient) instead of shouting. Every component reads from CSS variables defined in `dryl.css` — never hardcode colors, sizes, radii, shadows or durations. AI is treated as a first-class state of the UI: any AI-aware component accepts an `AiState` parameter (`None / Active / Thinking / Streaming / Generated`) that drives a shared visual vocabulary — rotating gradient border, breathing glow, one-shot reveal — so a user can feel where the AI is at work across the entire library without ever reading a label.

The design system lives in three files:

- `dryl.css` — every token and every primitive (including the AI mode primitives)
- `DESIGN_TOKENS.md` — readable reference of every token, when to use it
- `COMPONENT_PATTERNS.md` — how to structure a `.razor` component, including AI-aware components
- `CONVENTIONS.md` — the binding public-API naming rules (events, parameters, enums, slots) enforced for the 1.0 freeze

If a value is missing from those files, **do not invent it** — propose adding it to `dryl.css` as a new token and ask the maintainer to review.

---

## 2. Hard rules

These rules are non-negotiable. A PR that violates them should not be merged.

### 2.1 Tokens, not literals
✅ `background: var(--glass-1);`
❌ `background: rgba(255,255,255,0.03);`

The full list lives in `DESIGN_TOKENS.md`. Every color, every padding, every radius, every shadow, every duration and every easing curve must reference a CSS variable.

### 2.2 Two modes, one identity
DRYL renders in two color modes — dark and light — driven entirely by the token system. The default follows the operating system (`prefers-color-scheme`); apps and users can force a mode through `DrylThemeProvider` / `IDrylThemeService`.

- Components never branch on the mode. They consume tokens; the mode swaps the token values underneath them.
- Never write a mode-assuming literal (`rgba(255,255,255,…)`, hardcoded grays) in component CSS. If a value must differ per mode, it becomes a token with both values in `dryl.css` — added to **both** LIGHT-TOKEN-SET copies (`node scripts/check-light-sync.mjs` must stay green).
- Every new component is verified in **both** modes before it ships (flip `data-dryl-mode` on `<html>` in devtools).

### 2.3 Glass surfaces, not solid blocks
Cards, panels, modals → translucent with `backdrop-filter`. Never paint a solid hex on a card background.

### 2.4 Accents glow, never scream
Saturated accent colors are only ever used as:
- Gradients (`var(--accent-grad)`)
- 1px borders (`var(--accent-line)`)
- Glow rings (`box-shadow` with low alpha)
- Tiny dots and indicators

Never as a full background fill of a large surface.

### 2.5 Motion vocabulary is fixed
Three durations: `--dur-fast` (140ms), `--dur-med` (240ms), `--dur-slow` (420ms).
Three easings: `--ease-out`, `--ease-in-out`, `--ease-spring`.

Don't invent new ones. Don't use `linear`. Don't use durations under 100ms (feels glitchy) or over 600ms (feels broken).

### 2.6 Blazor naming
- **Components:** PascalCase, `Dryl` prefix → `DrylButton`, `DrylDataGrid`, `DrylInputText`.
- **CSS classes:** kebab-case, no prefix → `.btn`, `.glass-card`, `.badge-success`.
- **Files:** `DrylButton.razor` + `DrylButton.razor.cs` (if codebehind) + `DrylButton.razor.css` (if isolated styles).
- **Namespaces:** `DRYL.Components` (or sub-namespace by category).

### 2.7 Parameters are strongly typed
Use `enum` for variants, never `string`. Provide sensible defaults.

```csharp
[Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;
[Parameter] public ButtonSize Size { get; set; } = ButtonSize.Medium;
```

### 2.8 No external runtime dependencies
DRYL has zero npm packages, zero JS frameworks layered on top. If a component needs JS interop, put the script in `wwwroot/js/dryl.js` and inject `IJSRuntime`.

**Documented exception:** `Markdig` (BSD-2-Clause) is the **one** approved external runtime dependency — used by `DrylMarkdown` to parse Markdown server-side (raw HTML disabled for XSS safety). It was added with maintainer sign-off. This is the bar for any future dependency: a .NET NuGet only, never npm/JS, and only after the maintainer approves it here. Do not add others without the same approval.

### 2.9 Accessibility is not optional
- Every interactive element is keyboard-reachable.
- Every icon-only button gets `aria-label` and a `DrylTooltip`.
- `:focus-visible` must show the accent ring — already in `dryl.css`, just don't override `outline: none` without replacing it.
- Color contrast: body text on glass surfaces must be at least `var(--fg-muted)` (≈ 0.62 alpha on white); axial info text never below `var(--fg-dim)`.
- AI activity changes are announced via `aria-live="polite"` (already on `DrylAiIndicator` — mirror this when you build new AI-aware feedback).

### 2.10 AI mode is shared, not invented per component
DRYL has **one** AI vocabulary. Every AI-aware component re-uses it; no component invents its own.

- Use the shared `AiState` enum (`None / Active / Thinking / Streaming / Generated`). Do not add per-component states like `Loading`, `Generating`, `AiBusy`.
- The visual is delivered by the existing CSS primitives in `dryl.css`: `.ai-aura` + `.ai-aura-ring` + `.ai-aura-glow` + (optional) `.ai-aura-wash`, plus `.ai-indicator` for status pills.
- The opt-in parameter is always named `Ai` (of type `AiState`) and defaults to `AiState.None`. AI mode must be **off by default** so existing consumers see no change.
- Never invent a new AI animation, color, gradient, or duration. If you think you need one, propose adding it to `dryl.css` and ask the maintainer — same rule as 2.1.
- Components that semantically can't host AI mode (e.g. `DrylBadge`, `DrylToggle`) do not get an `Ai` parameter. Don't add it "just in case".

### 2.11 Icon-only buttons always have a tooltip
**Every** button that renders only an icon (no visible text label) **must** be wrapped in a `DrylTooltip` that names its action. No exceptions.

- This is both a usability and an accessibility requirement — a bare icon is ambiguous without a label on hover/focus.
- The tooltip text and the `aria-label` (see 2.9) should say the same thing.
- A button with visible text next to its icon does **not** need a tooltip; this rule is only for icon-*only* buttons.

✅ `<DrylTooltip Text="Delete row"><DrylButton IconOnly aria-label="Delete row"><DrylIcon Name="trash" /></DrylButton></DrylTooltip>`
❌ `<DrylButton IconOnly><DrylIcon Name="trash" /></DrylButton>`

### 2.12 Every component is animated — motion is not optional

DRYL feels *alive*. **Every new component MUST be deliberately animated** — never ship a component that just appears, snaps, or toggles with no transition. Aim for the polish of [motion.dev](https://motion.dev): smooth, physical, intentional.

Concretely, a new component must animate at least its relevant subset of:
- **Enter / exit** — appears and disappears with a transition, never instantly. Anything that mounts/unmounts conditionally (panels, overlays, list items, toasts) wraps in `DrylPresence` so it also animates *out*, not just in.
- **State changes** — hover, focus, active, selected, expanded, error → animated, not stepped (border-color, glow, transform — see rule 4's checklist).
- **Layout movement** — an active marker that moves between targets *glides* (use `dryl.motion.moveIndicator` / a shared indicator), it does not jump.
- **Reveal** — content-heavy or marketing surfaces use `DrylReveal` for staggered scroll-in where it fits.

Rules that still bind every animation:
- Only the fixed vocabulary — `--dur-fast|med|slow` and `--ease-out|in-out|spring` (rule 2.5). The motion.dev *feeling* comes from `--ease-spring` and good choreography, **not** from inventing new durations/easings.
- Reuse the shared motion primitives (`DrylPresence`, `DrylReveal`, `dryl.motion.*`, the `.presence-*` / `.reveal-*` / `.ai-aura*` classes). Do not hand-roll a one-off animation when a primitive exists — extend the primitive in `dryl.css` and ask the maintainer (same bar as rule 2.1).
- **Always** honour `prefers-reduced-motion: reduce` — the component must be fully usable with motion off (the primitives already do this; mirror it in any custom CSS).
- Animation is decorative only: it must never change focus order, keyboard reachability, or ARIA semantics. Moving indicators are `aria-hidden`.

If a component genuinely has nothing to animate, that is the rare exception — say so explicitly in its PR description. The default is: it moves.

---

## 3. How to build a new component

Follow this checklist for every new component:

1. **Find the closest match in `examples/`** and use it as a starting point. (`DrylButton`, `DrylCard`, `DrylBadge` cover ~80% of patterns.)
2. **Sketch the API first.** What parameters does the consumer pass in? Use `enum` for variants, `EventCallback<T>` for events, `RenderFragment` for slots.
3. **Write the markup using existing CSS classes from `dryl.css`** before writing any custom CSS. Most components need no custom CSS at all.
4. **If you must add CSS,** put it in `ComponentName.razor.css` (Blazor CSS isolation). Only reference tokens — never literals.
5. **Add the component to `DRYL.Components` namespace.** Add a `@using` to `_Imports.razor` if it's a new namespace.
6. **Provide an XML doc comment** on the class and on each `[Parameter]`. This is a library — IntelliSense matters.
7. **Add a one-page usage demo** under `samples/Pages/Demo<Component>.razor` showing every variant, size, and state.
8. **Verify in the prototype.** Open `prototype/DRYL Design System.html` and find a similar component on the Components page. The visual should match.

---

## 4. What "looking right" means

If you're unsure whether a component "feels DRYL", check it against these:

- [ ] Background is translucent, not solid
- [ ] Border is 1px and uses `var(--line)` or `var(--line-strong)`
- [ ] Hover state changes border-color, lifts shadow, or activates a glow — never just darkens the background
- [ ] Any animated property uses one of the three durations and one of the three easings
- [ ] Accent color appears only in: a gradient, a 1px border, a glow ring, or a small indicator
- [ ] Text on the surface uses `var(--fg)`, `var(--fg-muted)`, or `var(--fg-dim)` — never a hardcoded gray
- [ ] Radii use `var(--r-xs|sm|md|lg|xl|pill)` — never an arbitrary px value
- [ ] Padding uses `var(--sp-1..8)` — never an arbitrary px value
- [ ] Component reads correctly in **both color modes** (flip `data-dryl-mode` on `<html>` in devtools)

If 9/9 — ship it. If 7/9 — fix the two. If less — re-read this file.

---

## 5. What to ask the user before building

Before you start coding a new component, confirm:

1. **Component name** (PascalCase, `Dryl`-prefixed). Are we adding `DrylAutocomplete` or extending `DrylInputText`?
2. **Variants** — how many shapes does this come in? (e.g. Button → Primary / Secondary / Ghost / Danger.)
3. **Sizes** — Small / Medium / Large, or only one size?
4. **States** — does it need Loading? Disabled? Error? Empty?
5. **AI mode** — is this an AI-aware surface? If yes, it accepts the standard `Ai` parameter (`AiState`) and must support all five states without inventing new ones. If the answer is "not obviously" (e.g. `DrylBadge`, `DrylToggle`), the default is **no AI parameter**.
6. **Form-integration** — does it participate in `EditForm`? Should it implement `InputBase<T>`?
7. **Sample page** — should the demo go into the existing samples app, or do we need a new section?

If any of these are unclear, **ask** before writing code.

---

## 6. Things you should never do

- ❌ Invent a new color
- ❌ Hardcode a mode-assuming color instead of a token — every per-mode value lives in `dryl.css` (both LIGHT-TOKEN-SET copies)
- ❌ Use `!important` outside `__om-edit-overrides`
- ❌ Inline `style="..."` for values that have a token (one-offs are fine for layout: `style="grid-template-columns: 1fr 1fr;"` is OK; `style="color: #f4f4f7;"` is not)
- ❌ Add an external npm/JS library — DRYL has zero of them
- ❌ Write `<button>` instead of using a component — DRYL is the components
- ❌ Ship an icon-only button without a `DrylTooltip` — see rule 2.11
- ❌ Use emojis in component output (icons go through `DrylIcon`)
- ❌ Use `setTimeout` without `using IDisposable` cleanup
- ❌ Break public API of an existing component without a version bump
- ❌ Invent a per-component AI state enum (e.g. `ChatLoadingState`, `AiBusy`). Use `AiState` — see rule 2.10.
- ❌ Add a new AI animation or color. The five `AiState` values map to the existing `.ai-aura*` and `.ai-indicator` primitives. If you want a new visual, propose extending the primitive in `dryl.css`.
- ❌ Default `Ai` to anything other than `AiState.None`. AI styling must be opt-in.
- ❌ Ship a new component that snaps/appears with no animation, or that mounts/unmounts without an enter **and** exit transition — see rule 2.12.

---

## 7. Documentation maintenance — mandatory for every change

Every commit that touches library code **must** also update `CHANGELOG.md` and, where relevant, `README.md`. These two files are the public face of the library.

### 7.0 Versioning & release — you own the version

DRYL ships continuously. **You are the version owner**, not the maintainer. Every push to `main` is a potential release: the `Publish` workflow (`.github/workflows/publish.yml`) reads `<Version>` from `DRYL.Components/DRYL.Components.csproj`, and if no `v<Version>` tag exists yet, it builds, tests, packs and publishes that version to nuget.org, then tags it and cuts a GitHub Release.

The `<Version>` in the `.csproj` is the **single source of truth** that drives publishing. Therefore:

- **Whenever you touch library code, bump `<Version>` in the same commit.** Bug fix → **PATCH**, new component/parameter/feature → **MINOR**, breaking API change → **MAJOR**.
- If a change does **not** touch shippable library code (docs, samples, CI, tests only — see §7.3), leave the version alone. A push with an unchanged version finds the tag already present and is a clean no-op — nothing is published.
- Never publish by hand or push a `v*` tag yourself — the workflow owns tagging. Just bump the number and commit; the push does the rest.
- Keep `<Version>` and `CHANGELOG.md` in lockstep (see §7.1).

### 7.1 CHANGELOG.md

The file lives at the repository root and follows [Keep a Changelog](https://keepachangelog.com/) (v1.1.0) format with [Semantic Versioning](https://semver.org/).

Accumulate entries under `[Unreleased]` as you work. **When you bump `<Version>` (§7.0), cut a release in the changelog in the same commit:** rename the `[Unreleased]` block to `## [X.Y.Z] - YYYY-MM-DD` (the version you just set, today's date) and start a fresh, empty `[Unreleased]` above it. That keeps every published version traceable to its entries.

Pick the right sub-heading for each change:

| Sub-heading  | When to use                                                                      |
| ------------ | -------------------------------------------------------------------------------- |
| `Added`      | New component, new parameter, new CSS token, new service method                  |
| `Changed`    | Altered behaviour or API of an existing component (non-breaking)                 |
| `Deprecated` | Something that still works but will be removed in a future MAJOR version         |
| `Removed`    | Something deleted (only allowed in a MAJOR bump — coordinate with maintainer)    |
| `Fixed`      | Bug fix, visual regression, accessibility issue                                  |

**Entry format** — one bullet per logical change, component name in backticks:

```markdown
### Added
- `DrylSpinner` — New loading indicator; variants: Ring / Dots / Pulse; AI-Mode
- `DrylCard` — New `Elevation` parameter (`Low / Mid / High`) controls shadow depth
```

**Versioning rules** — you apply these yourself by bumping `<Version>` (§7.0):

| Change type              | Bump        |
| ------------------------ | ----------- |
| New component or feature | MINOR       |
| Bug fix / visual tweak   | PATCH       |
| Breaking API change      | MAJOR       |

### 7.2 Canonical component list

The canonical, browsable component list lives at **components.dryl.dev**, driven by the website's `ComponentCatalog` (in `DRYL.Website`). There is no component table in `README.md` — do not add one.

When you add a new component or make a user-visible change to an existing one:

1. **Register it in `ComponentCatalog`** in the website project — this is what powers the nav, search and overview page on the docs site.
2. **Add a changelog entry** under `[Unreleased]` in `CHANGELOG.md` (§7.1 above).

That is all. Do not maintain a duplicate list in `README.md` or any other markdown file.

### 7.3 What does NOT need a changelog entry

- Internal refactoring with no visible effect
- Changes to `samples/` demo pages only
- Typo fixes in comments or XML doc strings
- Changes to CI/build configuration

### 7.4 Checklist before you finish a task

Before considering any component work done, verify:

- [ ] `CHANGELOG.md` — entry added under `[Unreleased]` with the correct sub-heading
- [ ] `ComponentCatalog` in `DRYL.Website` — new component registered (or existing entry updated) so it appears on components.dryl.dev
- [ ] `DRYL.Components.csproj` — `<Version>` bumped for this change (PATCH/MINOR/MAJOR per §7.0) and in lockstep with the changelog release you cut
