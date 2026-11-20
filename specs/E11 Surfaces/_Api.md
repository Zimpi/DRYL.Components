# Surfaces — Public API

Shared enums, parameter contracts and services of the Surfaces category — the
part of the data contract the 1.0 freeze binds.

**Source folder:** `code/DRYL.Components/Components/Surfaces/`

This file carries no `Meta` block: it is a reference for the specs around it, not
a unit of implementation (`SPEC-03`).

The category holds eight components, each with its spec: `F1 DrylPopover.md`,
`F2 DrylToast.md`, `F3 DrylCard.md`, `F4 DrylChat.md`, `F5 DrylChatComposer.md`,
`F6 DrylDepthGlass.md`, `F7 DrylMarkdown.md` and `F8 DrylMessage.md`. The public
types they own are listed below.

## `PopoverPlacement`

Declared **top-level**, not nested in a component, in
`code/DRYL.Components/Components/Surfaces/PopoverPlacement.cs`. A consumer
therefore writes it unqualified after the usual `@using` —
`Placement="PopoverPlacement.TopEnd"` — which is how every call site in this
repository and in `DRYL.Website` spells it.

| Member | Notes |
|---|---|
| `BottomStart` | Below the trigger, leading edges aligned. The default of `DrylPopover.Placement`. |
| `BottomEnd` | Below the trigger, trailing edges aligned. |
| `TopStart` | Above the trigger, leading edges aligned. |
| `TopEnd` | Above the trigger, trailing edges aligned. |

The enum names a **preference**, not a guarantee: a panel whose preferred side
would overflow the viewport flips to the opposite side when there is room there,
and is clamped horizontally either way. `F1 DrylPopover.md` pins that; it is not
restated here.

A value outside the four members — reachable only by casting an out-of-range
integer — is treated as `BottomStart`, because both the placement token handed
to JS and the panel's modifier class fall through to the bottom-start case.

## `ToastVariant`

Top-level in `code/DRYL.Components/Toasts/ToastVariant.cs`, namespace
`DRYL.Components.Toasts`; owned by `F2 DrylToast.md`.

| Member | Notes |
|---|---|
| `Default` | No icon, no tint. The default of `DrylToast.Variant` and `ToastOptions.Variant`. |
| `Success` | `Check` icon, `--success` tint, polite. |
| `Warning` | `Alert` icon, `--warning` tint, assertive. |
| `Error` | `Alert` icon, `--danger` tint, assertive. |
| `Info` | `Info` icon, `--info` tint, polite. |

## `DrylCard.CardPadding`

Nested in `DrylCard` (declared in
`code/DRYL.Components/Components/Surfaces/DrylCard.razor`), so a consumer writes
it qualified: `Padding="DrylCard.CardPadding.Tight"`. Owned by `F3 DrylCard.md`.

| Member | Notes |
|---|---|
| `None` | No padding (`p-0`), for a custom layout. |
| `Tight` | `--sp-4` (`p-tight`). |
| `Default` | `--sp-5` (`p-default`). The default of `DrylCard.Padding`. |
| `Loose` | `--sp-6` (`p-loose`). |

A value outside the four members is rendered as `Default`.

## `DepthGlassIntensity`

Top-level in `code/DRYL.Components/DepthGlassIntensity.cs`, namespace
`DRYL.Components`. Owned by `F6 DrylDepthGlass.md`; also the type of
`DrylCard.Depth`.

| Member | Notes |
|---|---|
| `Subtle` | Gentle tilt and parallax (`dg--subtle`). |
| `Medium` | Balanced (`dg--medium`). The default of `DrylDepthGlass.Intensity`. |
| `Strong` | Deep tilt and pronounced parallax (`dg--strong`). |

A value outside the three members is rendered as `Medium`. On `DrylCard`, a `null`
`Depth` means "no depth warp" rather than any intensity.

## `MessageRole`

Top-level in `code/DRYL.Components/Components/Surfaces/MessageRole.cs`, namespace
`DRYL.Components`. Owned by `F8 DrylMessage.md`.

| Member | Notes |
|---|---|
| `User` | The human participant: row from the trailing edge, `--accent-soft` bubble. |
| `Assistant` | The AI or agent: row from the leading edge, glass bubble, default avatar icon `Sparkle`. The default of `DrylMessage.Role`. |
| `System` | A centred, muted status line: no avatar, no meta line, no bubble, no aura. |

A value outside the three members — reachable only by casting — is laid out and
styled as `Assistant`, but without its default `Sparkle` icon.

## Enums this category uses but does not own

Named so a consumer knows where to look, with their members deliberately not
restated — a second copy of a member list is the second source of truth
`SPEC-07` warns about.

| Type | Owner | Used by |
|---|---|---|
| `DrylMenu.MenuPlacement` | `E10 Navigation` — nested inside `DrylMenu` | Not by this category. Listed because `DrylMenu` maps its own placement onto `PopoverPlacement` when it configures the popover it wraps, so the two enums are read together. |
| `AiState` | `E1 Foundation` | `DrylCard`, `DrylChat`, `DrylChatComposer`, `DrylMarkdown`, `DrylMessage` and `DrylToast`, as the type of their `Ai` parameter. |
| `AiAura` | `E1 Foundation` | The same six, as the type of their `Aura` parameter — `AiAura?` (with `null` inheriting a surrounding `DrylAiScope`) on all but `DrylToast`, whose `Aura` is a non-nullable `AiAura` defaulting to `AiAura.Comet`. |

## The `Ai` parameter in this category

Four components inherit `Ai` and `Aura` from `DrylAiAware` — `DrylCard`,
`DrylChat`, `DrylMarkdown` and `DrylMessage` — and therefore resolve **both** the
state and the variant against a surrounding `DrylAiScope`. `DrylChatComposer`
declares the two parameters itself and resolves only the variant through the
scope: its state is `Ai` as given, a difference recorded in
`F5 DrylChatComposer.md`. `DrylToast` declares both itself and resolves neither
through a scope; see `F2 DrylToast.md`.

`DrylPopover` and `DrylDepthGlass` take no `AiState` and no `AiAura`: neither
declares an `Ai` parameter nor inherits `DrylAiAware`. Both are decisions rather
than omissions (`AI-05`), argued in `F1 DrylPopover.md` and
`F6 DrylDepthGlass.md`.

## The popover's contract with its consumers

`DrylPopover` is the primitive eight components in four other categories are
built on — `DrylMenu`, `DrylSelect`, `DrylMultiSelect`, `DrylAutocomplete`,
`DrylDatePicker`, `DrylTimePicker`, `DrylNotifications`, `DrylCitation`, plus
`DrylCanvasWorkspace` and the agents package's `DrylAiField`. Three parts of its
parameter surface are load-bearing for them and are named here because they are
a category-level contract rather than one component's detail:

| Parameter | What a consumer takes on by setting it |
|---|---|
| `TriggerTogglesOpen="false"` | The consumer drives the open state itself. Every input-shaped consumer does this, because a text field's own click, focus and key handling must decide when the panel opens. |
| `CloseOnEscape="false"` | The consumer owns `Escape` **completely** — closing *and* returning focus. Taking the key without ensuring focus reaches the element the handler is bound to leaves the panel with no keyboard dismissal at all; that was the defect in `DrylMenu` and in both pickers. |
| `Surface="false"` | The consumer paints the panel. Every input dropdown in the library does this and supplies its own listbox surface. |

`PanelRole` is the fourth: it is both the panel's ARIA role and the popup type
claimed on the trigger, so a consumer that passes none gets neither. The full
rule is in `F1 DrylPopover.md`.

## Services

**None.** No component in this category owns, registers or exposes a library
service, and `AddDrylComponents()` in
`code/DRYL.Components/Extensions/ServiceCollectionExtensions.cs` registers
nothing on their behalf: every entry it makes belongs to another category.
Five components in the folder — `DrylPopover`, `DrylCard`, `DrylChat`,
`DrylChatComposer` and `DrylDepthGlass` — inject `IJSRuntime`, which is the
framework's service rather than one of the library's, and none of them injects a
`DRYL.Components` service at all. See
[`_Interop.md`](_Interop.md).
