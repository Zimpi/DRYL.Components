# Code Rules

Binding code-level rules for DRYL. Component anatomy:
[`patterns.md`](patterns.md). Public API surface: [`conventions.md`](conventions.md).

Every rule has a stable ID. IDs are never reused: if a rule is dropped, its
number is burned. Gaps between number blocks are intentional — they leave room
for later rules without renumbering. The gap between `CODE-05` and `CODE-20`
separates hard code rules from process checklists.

**Status** — `binding` blocks the merge · `default` needs a reason in the PR ·
`guidance` is a recommendation.
**Enforced** — how compliance is established: `script`, `grep` or `review`.

---

### CODE-01 — Blazor naming

Status: **binding** | Enforced: **grep**

- **Public components:** PascalCase, `Dryl` prefix → `DrylButton`,
  `DrylTable`, `DrylInputText`.
- **CSS classes:** kebab-case, no prefix → `.btn`, `.glass-card`,
  `.badge-success`.
- **Files:** `DrylButton.razor` + `DrylButton.razor.cs` (if codebehind) +
  `DrylButton.razor.css` (if isolated styles).
- **Namespaces:** `DRYL.Components` (or sub-namespace by category).

**Internal building blocks deliberately carry no prefix.** A component a
consumer can neither place nor parameterise — one that lives under an
`Internal/` folder, or is reachable only through `internal` cascading
parameters — is not part of the public surface. The prefix is namespace
hygiene for someone else's code; on an internal part it only blurs the line
between what is public and what is not. The **absence** of the prefix is the
signal, and such components belong under an `Internal/` folder so the check
can see it.

Check: `find code -name '*.razor' -not -name '_*' -not -path '*/obj/*' -not -path '*/bin/*' -not -path '*/Internal/*' | grep -v '/Dryl'`
should return nothing — and **currently does**. The rule's one pre-existing hit
was `CanvasNodeView.razor`: internal in fact (it takes only `internal` cascading
parameters and is rendered solely by `DrylCanvas`) but sitting directly under
`Canvas/`, where the check could not tell it apart from a forgotten public
component. It moved to `Canvas/Internal/` on 2026-08-11 and the rule is clean.
It keeps `@namespace DRYL.Components.Canvas` — the folder is what this check
reads, not the namespace. See `docs/2026-08-11-red-rule-triage.md` and
`ideas/I3 Component folder layout.md`.

### CODE-02 — Parameters are strongly typed

Status: **binding** | Enforced: **review**

Use `enum` for variants, never `string`. Provide sensible defaults.

```csharp
[Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;
[Parameter] public ButtonSize Size { get; set; } = ButtonSize.Medium;
```

Check: no `[Parameter] public string Variant` (or equivalent stringly-typed
variant/size/kind parameter) exists (reviewer check). A supporting grep —
`grep -rn '\[Parameter\] public string.*Variant' code/` — currently returns
nothing, but a string-typed parameter can be named anything, so this is
review-enforced, not grep-enforced.

### CODE-03 — No external runtime dependencies

Status: **binding** | Enforced: **grep**

DRYL has zero npm packages, zero JS frameworks layered on top. If a component
needs JS interop, put the script in `wwwroot/js/dryl.js` and inject
`IJSRuntime`.

**Documented exception:** `Markdig` (BSD-2-Clause) is the **one** approved
external runtime dependency — used by `DrylMarkdown` to parse Markdown
server-side (raw HTML disabled for XSS safety). It was added with maintainer
sign-off. This is the bar for any future dependency: a .NET NuGet only, never
npm/JS, and only after the maintainer approves it here. Do not add others
without the same approval.

Check: `rg -n '<PackageReference' code/*/*.csproj` — currently **green**: all
`<PackageReference>` entries are either `Markdig` or `Microsoft.*` packages
(`Microsoft.Agents.AI`, `Microsoft.AspNetCore.Components.Web`).

### CODE-04 — XML doc comments

Status: **binding** | Enforced: **review**

Provide an XML doc comment on the class and on every `[Parameter]`. This is a
library; IntelliSense is the surface consumers see.

Check: reviewer confirms a `<summary>` on the component class and on every
public `[Parameter]` before merge — no automated scan documented yet.

### CODE-05 — Timers and interop handles are disposed

Status: **binding** | Enforced: **review**

Every `setTimeout`/`setInterval` started from `wwwroot/js/*.js` is paired with
a `clearTimeout`/`clearInterval` on teardown (component dispose, modal close,
retry/idle handlers).

Check: `rg -n 'setTimeout|setInterval' code/*/wwwroot/js/` against
`rg -n 'clearTimeout|clearInterval' code/*/wwwroot/js/` — currently **10** and
**12** hits respectively. The count alone does not prove pairing (3 of the 10
`setTimeout` hits are comments, not calls), so this is reviewer-enforced, not
grep-enforced: the grep pair is a starting point, and a human confirms each
named timer handle (`timerId`, `settle`, `state.retryTimer`,
`state.idleTimer`, `state.maxTimer`) is cleared on its teardown path. Manual
review confirms this holds, with one edge case: the modal `attach` function in
`dryl.js` uses an anonymous `setTimeout(fn, 0)` to move focus to the first
focusable element on the next tick, and holds no stored handle — not a leak in
practice, but also not an explicitly disposed handle.

---

### CODE-20 — How to build a new component

Status: **default** | Enforced: **review**

Follow this checklist for every new component:

1. **Find the closest match in `code/DRYL.Components/Components/`** and use it
   as a starting point. `DrylButton`, `DrylCard`, `DrylBadge` cover ~80% of
   patterns.
2. **Sketch the API first.** What parameters does the consumer pass in? Use
   `enum` for variants, `EventCallback<T>` for events, `RenderFragment` for
   slots.
3. **Write the markup using existing CSS classes from `dryl.css`** before
   writing any custom CSS. Most components need no custom CSS at all.
4. **If you must add CSS,** put it in `ComponentName.razor.css` (Blazor CSS
   isolation). Only reference tokens — never literals.
5. **Add the component to the `DRYL.Components` namespace.** Add a `@using`
   to `_Imports.razor` if it's a new namespace.
6. **Provide an XML doc comment** on the class and on each `[Parameter]`.
   This is a library — IntelliSense matters.
7. **Add a demo page in `DRYL.Website`** showing every variant, size and
   state, and register the component in its `ComponentCatalog` (`REL-04` in
   [`releasing.md`](releasing.md)). Demos live in the website, not in this
   repository — there is no samples project here.
8. **Verify in both color modes** against the rendered component on
   components.dryl.dev, and against the closest existing component in
   `code/DRYL.Components/Components/`. The visual must sit in the same family.

Check: reviewer walks the eight steps against the PR before approving.

### CODE-21 — What to clarify before writing code

Status: **default** | Enforced: **review**

Before you start coding a new component, confirm:

1. **Component name** (PascalCase, `Dryl`-prefixed). Are we adding
   `DrylAutocomplete` or extending `DrylInputText`?
2. **Variants** — how many shapes does this come in? (e.g. Button → Primary /
   Secondary / Ghost / Danger.)
3. **Sizes** — Small / Medium / Large, or only one size?
4. **States** — does it need Loading? Disabled? Error? Empty?
5. **AI mode** — is this an AI-aware surface? If yes, it accepts the standard
   `Ai` parameter (`AiState`) and must support all five states without
   inventing new ones. If the answer is "not obviously" (e.g. `DrylBadge`,
   `DrylToggle`), the default is **no AI parameter**. See `ai.md`.
6. **Form-integration** — does it participate in `EditForm`? Should it
   implement `InputBase<T>`?
7. **Demo page** — where does the demo go in `DRYL.Website`: an existing
   section or a new one? See `SPEC-05` in `requirements.md`.

If any of these are unclear, **ask** before writing code.

Check: reviewer confirms these seven points were raised (in the issue, PR
description, or chat) before implementation started — no automated scan
documented yet.
