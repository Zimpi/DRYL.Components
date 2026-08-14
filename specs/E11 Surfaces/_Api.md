# Surfaces — Public API

Shared enums, parameter contracts and services of the Surfaces category — the
part of the data contract the 1.0 freeze binds.

**Source folder:** `code/DRYL.Components/Components/Surfaces/`

This file carries no `Meta` block: it is a reference for the specs around it, not
a unit of implementation (`SPEC-03`).

**Partial by design, and this is the honest state of it.** The category holds
eight components — `DrylPopover`, `DrylCard`, `DrylChat`, `DrylChatComposer`,
`DrylDepthGlass`, `DrylMarkdown`, `DrylMessage` and `DrylToast`. Exactly one of
them has a spec today (`F1 DrylPopover.md`), and only what that component owns
or touches is written below. `MessageRole` in
`code/DRYL.Components/Components/Surfaces/MessageRole.cs` is the category's other
public type and is deliberately **not** documented here: it belongs to
`DrylMessage`, which has no spec yet, and a member list written without the spec
that pins its behaviour would be a second source of truth from the day it was
written. The remaining seven components are open, not covered.

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

## Enums this category uses but does not own

Named so a consumer knows where to look, with their members deliberately not
restated — a second copy of a member list is the second source of truth
`SPEC-07` warns about.

| Type | Owner | Used by |
|---|---|---|
| `DrylMenu.MenuPlacement` | `E10 Navigation` — nested inside `DrylMenu` | Not by this category. Listed because `DrylMenu` maps its own placement onto `PopoverPlacement` when it configures the popover it wraps, so the two enums are read together. |

`DrylPopover` takes no `AiState` and no `AiAura`: it declares no `Ai` parameter
and does not inherit `DrylAiAware`. That is a decision rather than an omission
(`AI-05`) and is argued in `F1 DrylPopover.md`.

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
Several components in the folder — `DrylPopover` among them — inject
`IJSRuntime`, which is the framework's service rather than one of the library's,
and none of them injects a `DRYL.Components` service at all. See
[`_Interop.md`](_Interop.md).
