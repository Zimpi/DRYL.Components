# UI/UX Rules

Binding accessibility and interaction rules for DRYL. Visual rules:
[`design.md`](design.md). Component-level code rules: [`code.md`](code.md).
AI-specific announcement rules (how `AiState` changes are surfaced to
assistive tech beyond the baseline set here) live in `ai.md`.

Every rule has a stable ID. IDs are never reused: if a rule is dropped, its
number is burned. Gaps between number blocks are intentional — they leave room
for later rules without renumbering.

**Status** — `binding` blocks the merge · `default` needs a reason in the PR ·
`guidance` is a recommendation.
**Enforced** — how compliance is established: `script`, `grep` or `review`.

---

### UX-01 — Every interactive element is keyboard-reachable

Status: **binding** | Enforced: **review**

Every interactive element is keyboard-reachable — buttons, links, custom
controls, anything a mouse/pointer user could activate must also be reachable
and operable via `Tab` / `Shift+Tab` and activated via `Enter` / `Space` (or
the control's native key, e.g. arrow keys for a slider).

Check: manual Tab pass through the component in both light and dark mode —
every interactive element receives visible focus in DOM order and every
action reachable by pointer is also reachable by keyboard (reviewer check; no
automated scan documented yet).

### UX-02 — `:focus-visible` shows the accent ring

Status: **binding** | Enforced: **review**

`:focus-visible` must show the accent ring — already wired in `dryl.css`.
Don't override `outline: none` without replacing it with an equivalent
visible focus indicator (a `box-shadow` ring, a `border-color` change tied to
`:focus`/`:focus-visible`, etc.).

Check: `rg -n 'outline:\s*none' code/` locates every candidate — currently
**36 hits** (13 in `dryl.css`, 23 across component `.razor.css` files). A
raw count does not establish compliance: the rule is about whether each
`outline: none` is paired with a replacement ring, and that pairing does not
sit in a fixed position relative to the `outline: none` — it can sit on an
entirely different selector governed by a different pseudo-class. The
`.input`/`.textarea`/`.select` group in `dryl.css` is the concrete case: the
base rule `.input, .textarea, .select { … outline: none; }` sets
`outline: none` unconditionally, and the replacement ring is **not**
adjacent to it — it lives on the separate rule
`.input:focus, .textarea:focus, .select:focus { … }`, which sets
`border-color` and a three-layer `box-shadow` glow. A **second**, deliberate
`outline: none` then sits on
`.input:focus-visible, .textarea:focus-visible, .select:focus-visible { outline: none; }`,
with its own comment explaining why: "The global `:focus-visible` rule adds
a bright cyan outline that clashes with the violet glow above. Suppress it
here — the glow already satisfies WCAG focus visibility." So one selector
family accounts for two of the 36 grep hits, and the ring that justifies
both lives on a third, unrelated-by-pseudo-class rule (`:focus`, not
`:focus-visible`) that a same-line or same-block grep would never associate
with either hit. (No line numbers are cited here on purpose — the ring rule
and the two suppressions move every time `dryl.css` is edited above them; a
selector is the only citation that survives that.)

This is exactly why a stronger paired check does not work cleanly here: a
"does this `outline: none` selector have a `:focus`/`:focus-visible` sibling
rule containing `box-shadow` in the same file" grep was tried against
`dryl.css` and produces both false negatives (a bare class token like
`.select` also appears in unrelated rules earlier in the file, so "does
`box-shadow` appear anywhere near a later occurrence of the class name" is
not a reliable signal) and misses the `.input`/`.select` case's actual shape
(the ring sits on a *different* pseudo-class variant of a *multi-selector*
group, not on the same selector as the `outline: none` it replaces). No
grep pattern tested reliably separates "replaced" from "not replaced"
without manual reading. This is why the check stays **review-enforced**,
not grep-enforced: the grep is the starting point that locates every
candidate line, and a human reads the surrounding rule to confirm a
replacement ring exists. Spot-checked in `dryl.css`: the
`.input`/`.textarea`/`.select` group above, plus
`.sidebar-toggle:focus-visible`, `.drawer-toggle:focus-visible`,
`.tbl-edit-btn:focus-visible`, `.toast-close:focus-visible`,
`.tab:focus-visible`, and `.dialog-close:focus-visible` — each of these six
carries its `outline: none` and its `box-shadow`/`border-color` replacement
in the same rule block. The full set of 36 has not been individually
re-verified for this document and should be walked during phase C.

### UX-03 — Contrast floor

Status: **binding** | Enforced: **script**

Color contrast has a floor: body text on glass surfaces must be at least
`var(--fg-muted)` (≈ 0.62 alpha on white); axial info text never below
`var(--fg-dim)`.

Check: `node scripts/validate-light-contrast.mjs` — currently **green**, all
10 checked tokens `PASS` (`--fg (text)` 16.72:1, `--success` 4.06:1,
`--warning` 4.62:1, `--danger` 4.44:1, `--info` 4.93:1, `--chart-3` 4.82:1,
`--chart-4` 4.63:1, `--chart-5` 5.50:1, `--chart-6` 5.14:1, `--danger-fg`
5.95:1 — all above their stated minimums), exit 0.

### UX-04 — AI activity is announced via `aria-live="polite"`

Status: **binding** | Enforced: **review**

AI activity changes are announced via `aria-live="polite"`. The precedent is
`DrylAiIndicator` — mirror it when building new AI-aware feedback.

Check: in `code/DRYL.Components/Components/AI/DrylAiIndicator.razor`, the root
`<span>` that carries `role="status"` also carries `aria-live="polite"`, and
the component's header comment states the intent — `Sets aria-live="polite" so
state changes are announced to assistive tech.` New AI-aware feedback
components are reviewed against this precedent (reviewer check; no automated
scan documented yet).

### UX-05 — Icon-only buttons always have a tooltip and a matching `aria-label`

Status: **binding** | Enforced: **review**

**Every** button that renders only an icon (no visible text label) **must**
be wrapped in a `DrylTooltip` that names its action. No exceptions.

- This is both a usability and an accessibility requirement — a bare icon is
  ambiguous without a label on hover/focus.
- The tooltip text and the `aria-label` (`UX-01`'s keyboard-reachability
  baseline extends to naming, not just reaching) should say the same thing.
- A button with visible text next to its icon does **not** need a tooltip;
  this rule is only for icon-*only* buttons.

✅ `<DrylTooltip Text="Delete row"><DrylButton IconOnly aria-label="Delete row"><DrylIcon Name="trash" /></DrylButton></DrylTooltip>`
❌ `<DrylButton IconOnly><DrylIcon Name="trash" /></DrylButton>`

Check: reviewer confirms every icon-only button (no visible text label) is
wrapped in `DrylTooltip` and that the tooltip text and `aria-label` say the
same thing; buttons with visible text beside their icon are exempt (reviewer
check; no automated scan documented yet — a `DrylButton IconOnly` usage is
not reliably distinguishable from a wrapped one by grep alone since the
wrapper is an ancestor element, not an attribute on the button itself).

### UX-06 — `prefers-reduced-motion: reduce` is always honoured

Status: **binding** | Enforced: **grep**

Always honour `prefers-reduced-motion: reduce` — the component must be fully
usable with motion off. The shared motion primitives already do this; any
custom component CSS must mirror it.

Check: `rg -c 'prefers-reduced-motion' code/DRYL.Components/wwwroot/dryl.css`
— currently **22** (green, count > 0: `dryl.css` itself honours the media
query). This only proves the shared primitive file does its part; it does
not scan individual component `.razor.css` files for CSS that introduces new
motion outside the shared primitives without its own
`prefers-reduced-motion` mirror — that remains a reviewer check per
component.

### UX-07 — Animation never changes focus order, keyboard reachability or ARIA semantics

Status: **binding** | Enforced: **review**

Animation is decorative only: it must never change focus order, keyboard
reachability, or ARIA semantics. Moving indicators are `aria-hidden`.

Check: reviewer confirms (a) no animation reorders tab stops or removes an
element from the keyboard-reachable set while animating, (b) no animation
adds/removes/changes ARIA roles or states as a side effect, and (c) any
purely decorative moving indicator (e.g. a gliding active-tab marker, an
AI aura) carries `aria-hidden="true"` (reviewer check; no automated scan
documented yet).

---
