# Shared-element transitions between overview and detail

## Meta
- **State:** Ready

## Problem

Raised by the Product Owner on 2026-08-22 as a user story:

> Als .NET-Entwickler möchte ich eine Blazor-Komponente nutzen können, die es
> mir erlaubt, mit minimalem Code sanfte Shared-Element-Transitions zwischen
> einer Übersicht und einer Detailansicht umzusetzen, ohne mich selbst um
> komplexe Animationslogik oder Timing kümmern zu müssen. Die Komponente soll
> mir erlauben, einfach eine Transition-ID zu vergeben und vordefinierte
> Motion-Token zu nutzen, damit sich UI-Elemente beim Navigieren flüssig und
> konsistent in die Detailansicht überführen.

The named pain is real and specific: **the timing contract, not the animation.**
A consumer who wants a card to morph into a detail view today has to know that
`view-transition-name` must be unique at snapshot time, that the mutation has to
run inside `document.startViewTransition`, and that the browser must be told
when Blazor's render actually reached the DOM. That is the "komplexe
Animationslogik und Timing" the story wants to be rid of.

## What already exists

*(Tech Lead, before any solution is proposed — `IDEA-05` order.)*

The library is much further along here than the story assumes, and the idea has
to be cut against that, not against a blank page:

- **`IDrylViewTransition`** (`code/DRYL.Components/Motion/`) — a scoped service
  that runs a Blazor state change inside a same-document view transition, with
  a documented `SignalRendered()` contract, and a morph-free fallback for
  prerender, unsupported browsers and `prefers-reduced-motion`.
- **`dryl.viewTransition`** (`wwwroot/js/dryl.js`) — the JS bridge, including
  the lazily injected `#dryl-merge` filter and swallowed skip-rejections.
- **The full morph vocabulary in `dryl.css`** — `::view-transition-group(*)` on
  `--dur-slow` / `--ease-viscous`, the `dryl-depth` tier
  (`dryl-depth-clarify`), and the reduced-motion opt-out.
- **`DrylViewTransitionStyle`** — the two tiers (`Glide`, `DepthGlass`), whose
  own XML docs already name "shared-element morphs such as card→dialog".
- **Three call sites already wired**: `DrylCard.ViewTransitionName` +
  `ViewTransitionStyle`, the `DrylDialog` / `DrylDialogProvider` handoff
  (`DialogOptions.HandoffStyle`), and `DrylTable`'s row reorder.

So the motion vocabulary, the timing contract and the fallbacks are **done**.
Nothing in the story asks for a new token, a new duration, a new easing or a new
`AiState`. What is missing is narrower than the story implies — and one part of
it is harder.

## The two gaps

**Gap 1 — the shared element is only available on `DrylCard`.**
`ViewTransitionName` exists on exactly one component. Anything else — a table
row, an image, a heading, a `<div>` of the consumer's own — has to hand-write
the inline style, and there is no place to hang the `dryl-depth` class or the
`data-vt-depth` marker the JS looks for. This gap is small, cheap and squarely
what "einfach eine Transition-ID vergeben" asks for.

**Gap 2 — "beim Navigieren" is not covered at all, and is the hard half.**
`IDrylViewTransition.RunAsync` takes a mutate delegate that ends in
`StateHasChanged()` on *one* component that then reports its own
`OnAfterRender`. A real route change (`NavigationManager.NavigateTo`) is a
different shape: the `Router` tears down the overview page and builds the detail
page, and the component that must call `SignalRendered()` is a component that
did not exist when the transition started.

Three consequences, decided rather than discovered:

1. **A loading detail page freezes the overview.** The view transition holds the
   old frame until the new DOM is committed. If the detail page loads its data
   in `OnInitializedAsync`, either the whole app freezes for the duration of
   that load, or the morph lands on an empty skeleton. There is no third
   option — this is a property of the API, not of the implementation. *Settled:
   morph onto the skeleton (see Decisions).*
2. **Blazor Server pays circuit latency for this.** A route change is a round
   trip. On WASM the freeze is a few frames; on Server it is the RTT. The
   skeleton policy is what keeps this bounded.
3. **Cross-document view transitions do not apply.** `@view-transition
   { navigation: auto }` needs a real document navigation. Interactive Blazor
   does not do one, so the same-document path is the only path.

## Solution Idea

Settled on 2026-08-22: **Option C — staged.** Step 1 is the generic hull; the
route-level host is a second, separate step that builds on it.

### Step 1 — `DrylMorph`, the generic shared-element hull

A wrapper component that turns any content into a morph target:

- `Name` — the transition ID. The one thing the story asks for.
- `Style` — `DrylViewTransitionStyle`, default `Glide`.
- `As` — the rendered tag, default `div`, so the hull stays valid inside lists
  and tables (`li`, `tr`, `article`, `section`, …).
- `Active` — default `true`; set `false` on the entries of a long overview that
  are not the morph target.
- `ChildContent`, plus the merged `Class` parameter and attribute splatting the
  library's other wrappers carry.

It renders the `view-transition-name`, the `view-transition-class: dryl-depth`
and the `data-vt-depth` marker `DrylCard` renders today — and it reports its own
`OnAfterRender` to `IDrylViewTransition.SignalRendered()`, so the consumer never
writes that line.

### Step 2 — a route-level transition host *(not yet scoped)*

A component wrapping the `Router`'s content that hooks
`NavigationManager.RegisterLocationChangingHandler`, starts the transition
there, and lets the detail page's own `DrylMorph` (or its skeleton) close the
loop. Its behaviour rests on the skeleton policy decided below. It gets its own
scoping pass before anything is written.

## Scope

- **In scope (step 1):**
  - A new `DrylMorph` component with `Name`, `Style`, `As`, `Active`,
    `ChildContent`, merged `Class` and attribute splatting.
  - `DrylMorph` reporting its render to `IDrylViewTransition.SignalRendered()`
    from `OnAfterRender`, unconditionally — half of today's timing contract
    disappears from consumer code.
  - `DrylCard` delegating its `ViewTransitionName` / `ViewTransitionStyle`
    rendering to the same logic, so the inline-style construction exists once.
  - A demo page and `ComponentCatalog` entry showing overview → detail on one
    route (the shape that works today and is merely undiscoverable).
- **Out of scope (step 1):**
  - The route-level host. Decided in principle, scoped separately (step 2).
  - Any new token, duration, easing, `AiState` or dependency.
  - A second animation vocabulary — `DrylMorph` renders only what `dryl.css`
    already defines.
  - Automatic detection of which element is "the" morph target. Rejected as not
    reliably implementable.
  - A fully automatic trigger parameter (`On="@selectedId"`). Rejected: the hull
    cannot see when *other* components finished rendering, so it cannot honestly
    own the start of the transition.
  - Cross-document (`@view-transition { navigation: auto }`) transitions.

## Impact

*(Tech Lead, `IDEA-05`.)*

### Harness

- **Step 1:** no new token, no new animation/duration/easing, no new `AiState`,
  no new dependency — it renders values `dryl.css` already defines and
  `DrylCard` already uses. **No blocker.**
- **Step 2:** no new visual vocabulary either. The skeleton policy is new
  *behaviour* and belongs in its spec before it is code. **No harness blocker.**

### Specs

- No `E{n} Motion` category exists. Settled against `SPEC-02` (a component's
  category follows its source folder): the hull is a content wrapper a consumer
  places on a page, exactly like `DrylReveal` — the library's other motion
  wrapper, which lives in `Components/Layout/`. So `DrylMorph.razor` goes to
  `code/DRYL.Components/Components/Layout/` and its spec to
  `specs/E9 Layout/F17 DrylMorph.md`, appended after the sixteen existing
  Layout components (`SPEC-02`: numbers are appended, never inserted).
  `E1 Foundation` was considered and rejected: its source folder is
  `Components/Providers/`, which holds what a consumer mounts *once in the
  layout* — `DrylMorph` is placed per page, many times over.
- The category table in `harness/requirements.md` needs `E9 Layout` raised from
  16 to 17 and the total from 127 to 128 in the same commit, and the `127`
  named in `CLAUDE.md`'s verification section with it.
- The view-transition service is documented in the `_Interop.md` files of the
  categories that consume it (`E3 AI`, `E5 Data`, `E6 Dialogs`);
  `specs/E9 Layout/_Interop.md` gains it too.
- Touches `specs/E11 Surfaces/` (`DrylCard` delegates) and
  `specs/E6 Dialogs/_Interop.md` (the handoff description).

### Public API

- **Step 1:** one new component, five parameters. Additive → MINOR (`REL-01`).
  `DrylCard.ViewTransitionName` stays; it is post-1.0 API and is not renamed,
  only re-implemented on top of the shared logic.
- **Step 2:** one further component plus its policy parameter. Additive, MINOR.

### Code

- A new component under `code/DRYL.Components/Components/`;
  `DrylCard.razor` refactored to delegate.
- **Risk — duplicate names.** Two elements carrying the same
  `view-transition-name` at snapshot time make the browser skip the morph
  silently (`dryl.viewTransition` swallows the skip-rejection by design). This
  is the reason `Active` exists, and it needs a test rather than a screenshot.
- **Risk — which service instance is being signalled.** `DrylMorph` injects the
  DI-scoped `IDrylViewTransition`, but `DrylDialogProvider` and `DrylTable`
  deliberately run their own `DrylViewTransition` instances. Those two keep
  signalling themselves; a `DrylMorph` inside them signals the scoped service,
  where the call is a documented no-op when no transition is in flight. No
  conflict, but it must be written down so it is not "fixed" later.
- **Risk — the extra box.** The hull is a real element; `display: contents`
  cannot carry a `view-transition-name`. `As` keeps the DOM valid, but the hull
  still participates in the parent's layout, which the demo page must show
  honestly.

## Decisions

- 2026-08-22 (Tech Lead): the story is **not** taken at face value as "build a
  shared-element component". Most of what it asks for ships already; the idea is
  cut down to the two gaps above before any option is chosen.
- 2026-08-22 (Product Owner): **Option C — staged.** The generic hull first, the
  route-level host as a second, separately scoped step.
- 2026-08-22 (Product Owner): while a detail page loads, the transition
  **morphs onto the skeleton**. The UI is never frozen waiting for data; the
  detail page renders its `DrylSkeleton` and fills in afterwards.
- 2026-08-22 (Product Owner): the hull **takes over the reporting half** of the
  timing contract — the consumer calls `RunAsync`, `SignalRendered` is the
  hull's job.
- 2026-08-22 (Product Owner, delegated to the Tech Lead): the hull renders a
  **real element with a configurable tag (`As`, default `div`)** rather than a
  fixed `div` or a cascaded style the child must splat itself — it keeps the DOM
  valid in lists and tables without pushing work back onto the consumer.
- 2026-08-22 (Product Owner, delegated to the Tech Lead): long overviews are
  handled by an **`Active` parameter** (default `true`), not by naming every
  entry forever and not by automatic detection — the consumer keeps control, and
  duplicate names stay preventable.

## Open Points

*(none — awaiting the Product Owner's explicit confirmation of this final
version, the last box of `IDEA-06`, before the state moves to `Ready`.)*
