# DRYL View Transitions — Design

**Date:** 2026-07-11
**Branch:** `feat/view-transitions`
**Status:** Proposed (awaiting "fang an")

## Goal

Give DRYL native access to the browser's [View Transition API](https://developer.chrome.com/docs/web-platform/view-transitions)
so state changes can **morph** between two DOM states (FLIP-style: position, size,
opacity) instead of only playing keyframes on a single, unchanged element. This is
the gap the Motion Foundation spec explicitly left open:

> Out of scope (future tickets): Generic FLIP for list/table reordering.
> Card→Dialog shared-element transition.

Scope of this spec is **same-document view transitions** only (state changes within
a running Blazor circuit/WASM app). Cross-document view transitions (Blazor Web App
enhanced navigation, `@view-transition { navigation: auto }`) are explicitly out of
scope — the exact `blazor.web.js` hook points need separate verification and are not
promised here.

## Why this needs a new primitive, not just CSS

`document.startViewTransition(updateCallback)` requires the DOM to already reflect
the new state by the time `updateCallback`'s promise resolves — the browser snapshots
"old" before the call and "new" right after. Blazor's `StateHasChanged()` only
*queues* a render; it does not resolve synchronously with the DOM patch. So the
mutation must happen **inside** a JS↔.NET round trip that only resolves after
`OnAfterRender` has actually fired — the same handshake shape already used by
`DrylPresence.OnExitFinished` (JS waits for a Blazor lifecycle callback, not the
other way round).

## Motion character — "Depth Glass"

A generic UA cross-fade (the View Transition API's default) reads as "ok, it
didn't jump" — not as DRYL. Before touching architecture, here is the exact
qualitative target and, for each quality, the concrete CSS/JS mechanism that
produces it. Everything below is additive to — not a replacement of — the
Motion Foundation vocabulary (rule 2.5); it is scoped to view-transition
pseudo-elements only.

| Quality | What it means | Mechanism |
|---|---|---|
| **Viscous** | Resists starting, then glides with weight — syrup/molten glass, never a bouncy spring | One new easing, `--ease-viscous` (below) — no overshoot, slow onset, long controlled settle. Replaces `--ease-spring` as the default curve for `::view-transition-group()` |
| **Translucent** | Light diffuses through the material during the morph, not a flat opacity fade | A transient `blur()`/`saturate()` pulse on `::view-transition-new()`, decoupled from (faster than) the shape's settle — the surface looks like it's re-condensing out of glass, not fading in |
| **Mercury-like** | Merging/splitting shapes pull together with the surface tension of a mercury droplet | A CSS merge filter (`blur` + high-`contrast`, the technique commonly called a "goo filter") applied to `::view-transition-image-pair()` for the transition's lifetime only — an established web technique, not invented from scratch |
| **Crystalline** | Despite all of the above, the resting frame is perfectly sharp — never a lingering blur/smear | The blur/merge filters run on a *shorter* duration (`--dur-med`) than the shape glide (`--dur-slow`) and end in `filter: none` — clarity always arrives before the shape finishes settling |

### One new token: `--ease-viscous`

```css
--ease-viscous: cubic-bezier(0.45, 0.05, 0.15, 1);
```

None of the three existing easings model this: `--ease-spring` overshoots (bounce —
the opposite of viscous), `--ease-out` accelerates fast then decelerates (snappy,
not draggy), `--ease-in-out` is a symmetric, comparatively "thin" curve. Proposed
as the one exception to rule 2.5's fixed easing set — same precedent as
`--reveal-step` in the Motion Foundation spec (one new token, narrowly scoped,
documented in `DESIGN_TOKENS.md`, maintainer sign-off = this spec). It is scoped
to view-transition pseudo-elements; it does not replace `--ease-spring` anywhere
else (`DrylTabs` indicator glide, etc. are untouched).

No new duration token: the two-speed choreography ("shape settles slower than
surface clears") is built entirely from the existing `--dur-med` (240ms, clarity)
and `--dur-slow` (420ms, shape) — staying inside rule 2.5's 100–600ms bounds.

### `DrylViewTransitionStyle` — how much "Depth Glass" a given morph gets

Applying the full merge-filter/blur choreography to *every* transition (e.g. a
high-frequency table-row drag) would feel heavy, not premium — the full
vocabulary is a **signature** for meaningful merges (card→dialog, a badge
absorbing into or splitting off a pill), not an ambient tic for every reorder.
So the character is split into two tiers, both using `--ease-viscous` (rule: it
never feels generically "ok", even at the light end) but only `DepthGlass` pays
for the blur/merge pass:

```csharp
public enum DrylViewTransitionStyle
{
    /// <summary>Viscous easing only — shape glides, no blur/merge pass. Cheap
    /// enough for high-frequency interactions (table row reorder, list re-sort).</summary>
    Glide,

    /// <summary>Full "Depth Glass" choreography — translucency pulse + mercury
    /// merge filter + decoupled crystalline clarity. Reserved for low-frequency,
    /// high-meaning merges (shared-element morphs).</summary>
    DepthGlass
}
```

## Architecture

One JS module (same conventions as `dryl.motion`: WeakMap state, `reduced()` guard,
feature detection), one DI service, a small `ViewTransitionName` + `ViewTransitionStyle`
opt-in on components that act as morph targets. One new token (`--ease-viscous`,
above), no new AI states.

### 1. `window.dryl.viewTransition` (new JS module, `wwwroot/js/dryl.js`)

```js
window.dryl.viewTransition = (() => {
    function start(dotNetRef) {
        if (!document.startViewTransition || reduced()) {
            // No support, or user opted out of motion: apply the change directly,
            // no snapshot/morph — same fallback shape as dryl.motion.onExit.
            return dotNetRef.invokeMethodAsync('ApplyChange');
        }
        const t = document.startViewTransition(() => dotNetRef.invokeMethodAsync('ApplyChange'));
        // t.ready / t.finished are available for future JS-side hooks; not consumed yet.
        return t.finished;
    }
    return { start };
})();
```

`reduced()` reuses the exact same `matchMedia('(prefers-reduced-motion: reduce)')`
check already private to `dryl.motion` — hoisted so both modules share it instead of
duplicating the guard.

### 2. `IDrylViewTransition` (new C# service, DI-scoped)

```csharp
public interface IDrylViewTransition
{
    Task RunAsync(Action mutate);
    Task RunAsync(Func<Task> mutate);
}
```

Implementation does the handshake:

```csharp
internal sealed class DrylViewTransition : IDrylViewTransition, IDisposable
{
    private TaskCompletionSource? _renderTcs;
    private Func<Task>? _pending;
    private readonly DotNetObjectReference<DrylViewTransition> _selfRef;

    public Task RunAsync(Action mutate) => RunAsync(() => { mutate(); return Task.CompletedTask; });

    public async Task RunAsync(Func<Task> mutate)
    {
        _pending = mutate;
        await JS.InvokeVoidAsync("dryl.viewTransition.start", _selfRef);
    }

    [JSInvokable]
    public async Task ApplyChange()
    {
        _renderTcs = new TaskCompletionSource();
        if (_pending is not null) await _pending();
        // Caller's StateHasChanged() (inside mutate) has queued a render;
        // resolve only once it actually reaches the DOM.
        await _renderTcs.Task;
    }

    // Called from a root RenderTree hook — see "the render-signal problem" below.
    internal void SignalRendered() => _renderTcs?.TrySetResult();
}
```

**The render-signal problem:** `IDrylViewTransition` is a plain service, not a
component — it has no `OnAfterRender`. The consuming component must call
`StateHasChanged()` inside `mutate` and additionally tell the service when *its own*
`OnAfterRender` fires:

```csharp
protected override void OnAfterRender(bool firstRender) => _viewTransition.SignalRendered();
```

This is one extra line per consuming component (documented requirement, mirrors the
existing `OnAfterRender` boilerplate `DrylPresence`/`DrylReveal` already carry) rather
than a hidden magic wrapper — keeps the mental model explicit: *you still own
`StateHasChanged`, the service just tells the browser when to snapshot.*

### 3. `ViewTransitionName` parameter

Opt-in on components that can be a morph endpoint (`DrylTable` rows initially, later
candidates: `DrylCard`, `DrylDialog`, `DrylListItem`):

```csharp
[Parameter] public string? ViewTransitionName { get; set; }
[Parameter] public DrylViewTransitionStyle ViewTransitionStyle { get; set; } = DrylViewTransitionStyle.Glide;
```

Renders `style="view-transition-name: {value}"` plus, when `DepthGlass`, tags the
pseudo-element into the `dryl-depth` CSS group below (via Chromium's
`view-transition-class`, with a JS-maintained fallback name-list where that
property isn't supported yet — see the open question in §4). Must be unique among
elements simultaneously in the DOM — a duplicate name silently voids the *entire*
transition (Chromium throws, transition skips). DRYL does not auto-generate names
(would risk collisions across component instances); the consumer supplies a stable
per-row id, same discipline as `@key`.

### 4. CSS in `dryl.css`

`Glide` re-points the UA default pseudo-elements at the viscous easing. `DepthGlass`
layers the translucency pulse + mercury merge + decoupled crystalline clarity on
top, scoped to the `dryl-depth` transition class:

```css
::view-transition-group(*) {
  animation-duration: var(--dur-slow);
  animation-timing-function: var(--ease-viscous);
}

/* DepthGlass: mercury-like merge. A merge filter (blur + high-contrast) pulls the
   old/new snapshots together with a mercury droplet's surface tension instead of
   a flat cross-fade. Filter is transient — see the crystalline clarify pass below. */
::view-transition-image-pair(dryl-depth) {
  filter: url(#dryl-merge);
}

/* DepthGlass: translucency + crystalline clarity. Runs on --dur-med (faster than
   the --dur-slow shape glide above) so the surface is always sharp before the
   shape finishes settling — "the glass clears before the motion finishes". */
@keyframes dryl-depth-clarify {
  from { filter: blur(6px) saturate(1.35); }
  to   { filter: blur(0) saturate(1); }
}
::view-transition-new(dryl-depth) {
  animation: dryl-depth-clarify var(--dur-med) var(--ease-out) both;
}

@media (prefers-reduced-motion: reduce) {
  ::view-transition-group(*),
  ::view-transition-old(*),
  ::view-transition-new(*),
  ::view-transition-image-pair(*) {
    animation: none !important;
    filter: none !important;
  }
}
```

The `#dryl-merge` SVG filter (`feGaussianBlur` + `feColorMatrix` contrast boost —
the technique sometimes called a "goo filter") is injected once as a
visually-hidden `<svg>` by `dryl.viewTransition` the first time a `DepthGlass`
transition runs, mirroring the lazy-DOM-injection pattern `dryl.popover` already
uses for portaling.

**Open question, to verify during implementation, not before:** browsers animate
`::view-transition-group()`'s `transform` internally to interpolate position/size;
it is not yet confirmed whether layering our own `transform` (for a squash/stretch
"pinch" accent) composes safely via `animation-composition: add` or fights the UA
animation. If it fights, the pinch accent is dropped and the merge filter alone
carries the "mercury" read — the filter technique above does not depend on this
and ships regardless.

## First retrofit / proof of concept: `DrylTable` row reorder

`DrylTable` already has drag-to-reorder (`ReorderColumnVisible`, `OnDragStart` /
`OnDrop`, rows keyed with `@key="item"`) and click-to-sort — both currently *snap*
rows to their new index. This is the cleanest first target: rows are already
individually keyed, the mutation point is a single method (`OnDrop`, `ApplySort`),
and there is no body-portal timing to fight (unlike Menu/Popover).

- New opt-in `[Parameter] public bool AnimateReorder { get; set; }` (default `false`
  — additive, non-breaking, matches the "opt-in until proven" posture of
  `DrylPresence.Appear`). When `true`:
  - Each row gets `ViewTransitionName="tbl-row-{rowKeySelector(item)}"` internally —
    `DrylTable` already requires a stable identity for `@key`; reuse whatever the
    table uses for equality (falls back to `item.GetHashCode()` string like the
    existing detail-row `@key="@($"d:{item!.GetHashCode()}")"` does today, with a
    documented caveat: hash-code-based names are only stable if `TItem` doesn't
    override `GetHashCode()` per-instance mutation — recommend consumers pass an
    explicit `RowIdSelector` if precise morphing matters).
  - `OnDrop` / the sort-apply path calls `_viewTransition.RunAsync(() => { ...
    reorder _view ...; StateHasChanged(); })` instead of mutating directly.
- Rows use `ViewTransitionStyle.Glide` (the default) — viscous easing, no merge/blur
  pass. A high-frequency drag interaction should feel weighty, not laggy;
  `DepthGlass`'s translucency/merge pass is reserved for the card↔dialog retrofit
  (next), where a shape actually appears to merge or split rather than just sliding.

## Design principle carried forward

Every future `ViewTransitionName` retrofit picks a style deliberately: `Glide` for
anything frequent or purely positional (reorder, resort, tab switch), `DepthGlass`
for rare, high-meaning shape merges (card→dialog, a badge absorbing into a pill).
This mirrors the "signature vs. ambient" split DRYL already makes for the `Ai` vocabulary
(rule 2.10) — one shared vocabulary, applied with restraint, not on every element.

## Accessibility

- `prefers-reduced-motion: reduce` fully disables the morph (CSS `@media` block +
  JS `reduced()` short-circuit that skips `startViewTransition` entirely) — rows
  reorder instantly, no snapshot cost paid.
- Purely visual: focus, tab order, ARIA (`aria-selected`, roles) on `DrylTable` are
  unaffected — the view transition only wraps the same DOM mutation that already
  happens, it does not change what is keyboard-reachable.
- No screen-reader announcement needed beyond what `DrylTable` already provides;
  this is a "how it moves," not "what happened" change.

## Out of scope (this spec)

- Cross-document / Blazor Web App enhanced-navigation view transitions — needs
  separate verification of `blazor.web.js` hook points before promising it.
- `DrylDialog` card→dialog shared-element morph, `DrylCard`/`DrylListItem` retrofits
  — natural follow-ups once the service + `DrylTable` PoC prove the handshake in
  practice. This is also the earliest realistic target for
  `ViewTransitionStyle.DepthGlass` — `DrylTable` reorder only proves `Glide`.
- Auto-generated `ViewTransitionName`s — deferred until there's a clear, collision-
  safe default.
- The `transform`/`animation-composition: add` squash-stretch "pinch" accent (§4,
  open question) — ships only if it composes cleanly with the UA group animation;
  otherwise the merge filter alone carries the mercury read.

## Browser support caveat

Same-document View Transitions: Chrome/Edge 111+, Safari 18+, Firefox 144+ (recent).
No polyfill — `dryl.viewTransition.start` always has the direct-apply fallback, so
the feature never blocks unsupported browsers; it just doesn't morph there.

## Docs / definition of done (CLAUDE.md §7)

- `DESIGN_TOKENS.md` — document `--ease-viscous`: what it is, when to use it
  (view-transition pseudo-elements only), why it's distinct from `--ease-spring`.
- `CHANGELOG.md` `[Unreleased]`: Added (`IDrylViewTransition`, `dryl.viewTransition`,
  `ViewTransitionName`, `ViewTransitionStyle`, `DrylViewTransitionStyle`,
  `--ease-viscous` token), Changed (`DrylTable` — new `AnimateReorder` parameter).
- XML docs on the service interface/methods and the new parameters.
- Demo: extend the existing `DemoTable`/reorder sample to show `AnimateReorder`.
- `ComponentCatalog` — no new component, so no new entry; existing `DrylTable`
  entry's description may mention it.
