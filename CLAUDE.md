# Instructions for Claude (and any AI agent) — DRYL Component Library

You are helping build **DRYL**, an open-source UI component library for Blazor Server and Blazor WebAssembly. Your job is to produce new components that are **visually consistent**, **token-driven**, and **idiomatic Blazor**.

Read this file before doing any work. Read it again if you find yourself inventing a color, a spacing value, or a one-off animation.

---

## 1. The system in one paragraph

DRYL is **dark, glassy, alive — and AI-native**. Surfaces are translucent layers stacked on pure black. Accents glow (violet → cyan gradient) instead of shouting. Every component reads from CSS variables defined in `dryl.css` — never hardcode colors, sizes, radii, shadows or durations. AI is treated as a first-class state of the UI: any AI-aware component accepts an `AiState` parameter (`None / Active / Thinking / Streaming / Generated`) that drives a shared visual vocabulary — rotating gradient border, breathing glow, one-shot reveal — so a user can feel where the AI is at work across the entire library without ever reading a label.

The design system lives in three files:

- `dryl.css` — every token and every primitive (including the AI mode primitives)
- `DESIGN_TOKENS.md` — readable reference of every token, when to use it
- `COMPONENT_PATTERNS.md` — how to structure a `.razor` component, including AI-aware components

If a value is missing from those three files, **do not invent it** — propose adding it to `dryl.css` as a new token and ask the maintainer to review.

---

## 2. Hard rules

These rules are non-negotiable. A PR that violates them should not be merged.

### 2.1 Tokens, not literals
✅ `background: var(--glass-1);`
❌ `background: rgba(255,255,255,0.03);`

The full list lives in `DESIGN_TOKENS.md`. Every color, every padding, every radius, every shadow, every duration and every easing curve must reference a CSS variable.

### 2.2 Dark first, only dark
The library has no light theme. Don't add `prefers-color-scheme` overrides. Don't add a `--light-bg` variable. Dark is the design — not a toggle.

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

If 8/8 — ship it. If 6/8 — fix the two. If less — re-read this file.

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
- ❌ Add a light theme
- ❌ Use `!important` outside `__om-edit-overrides`
- ❌ Inline `style="..."` for values that have a token (one-offs are fine for layout: `style="grid-template-columns: 1fr 1fr;"` is OK; `style="color: #f4f4f7;"` is not)
- ❌ Add an external npm/JS library — DRYL has zero of them
- ❌ Write `<button>` instead of using a component — DRYL is the components
- ❌ Use emojis in component output (icons go through `DrylIcon`)
- ❌ Use `setTimeout` without `using IDisposable` cleanup
- ❌ Break public API of an existing component without a version bump
- ❌ Invent a per-component AI state enum (e.g. `ChatLoadingState`, `AiBusy`). Use `AiState` — see rule 2.10.
- ❌ Add a new AI animation or color. The five `AiState` values map to the existing `.ai-aura*` and `.ai-indicator` primitives. If you want a new visual, propose extending the primitive in `dryl.css`.
- ❌ Default `Ai` to anything other than `AiState.None`. AI styling must be opt-in.

---

## 7. Documentation maintenance — mandatory for every change

Every commit that touches library code **must** also update `CHANGELOG.md` and, where relevant, `README.md`. These two files are the public face of the library.

### 7.1 CHANGELOG.md

The file lives at the repository root and follows [Keep a Changelog](https://keepachangelog.com/) (v1.1.0) format with [Semantic Versioning](https://semver.org/).

**Always write into `[Unreleased]`** — never create a new version section; that is the maintainer's job at release time.

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

**Versioning rules** (for maintainer, but good to know):

| Change type              | Bump        |
| ------------------------ | ----------- |
| New component or feature | MINOR       |
| Bug fix / visual tweak   | PATCH       |
| Breaking API change      | MAJOR       |

### 7.2 README.md — component table

The table in the **"What's in the box (today)"** section of `README.md` must reflect every component in the library. When you add or change a component:

1. **New component** → add a row with: name, category, AI mode (✅ or —), status (✅ Done), short notes (≤ 12 words describing the key features).
2. **Changed component** → update the notes column if the change is user-visible.
3. **Removed component** → remove the row.

**Do not** rewrite or reformat unrelated rows.

### 7.3 What does NOT need a changelog entry

- Internal refactoring with no visible effect
- Changes to `samples/` demo pages only
- Typo fixes in comments or XML doc strings
- Changes to CI/build configuration

### 7.4 Checklist before you finish a task

Before considering any component work done, verify:

- [ ] `CHANGELOG.md` — entry added under `[Unreleased]` with the correct sub-heading
- [ ] `README.md` — component table row added / updated if component is new or its public API changed
- [ ] `DRYL.Components.csproj` — `<Version>` is still consistent with the changelog (maintainer sets this; don't bump without being asked)
