# Dialogs — Public API

Shared enums, parameter contracts and services of the Dialogs category — the
part of the data contract the 1.0 freeze binds.

**Source folder:** `code/DRYL.Components/Dialogs/`

The category is service-driven and that shapes its API. A consumer does not
place a dialog on a page; they mount `DrylDialogProvider` once in the root
layout, inject `IDrylDialogService`, and call `ShowAsync` for a dialog *type*.
The types below are what travels between those two ends. The component specs
describe behaviour; this file describes the contract.

Everything here lives in the `DRYL.Components.Dialogs` namespace, except
`DrylDialog`, `DrylDialogProvider`, `DrylAlertDialog` and `DrylConfirmDialog`
themselves.

## `DialogSize`

Width preset of a dialog. An `enum`.

| Value | Meaning |
|---|---|
| `Small` | Compact — confirmations and alerts. |
| `Medium` | The default — forms and content. |
| `Large` | Wide — rich content, multi-column layouts. |
| `Fullscreen` | Fills the viewport — immersive flows. |

Each value maps to one CSS class on the dialog. The widths themselves live in
`code/DRYL.Components/wwwroot/dryl.css` and are documented in
[`../../harness/tokens.md`](../../harness/tokens.md), never repeated in a spec
(`SPEC-07`).

## `DialogOptions`

Per-call configuration, passed to `ShowAsync`. A `sealed class` with settable
properties; every one has a default, so `new DialogOptions()` is a valid call.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Size` | `DialogSize` | `DialogSize.Medium` | Width preset. |
| `CloseOnEscape` | `bool` | `true` | `Escape` cancels the dialog. |
| `CloseOnBackdropClick` | `bool` | `true` | A click outside the dialog cancels it. |
| `ShowCloseButton` | `bool` | `true` | The header shows a close button. |
| `Ai` | `AiState` | `AiState.None` | Initial AI state of the dialog frame. |
| `Class` | `string?` | `null` | Extra CSS class applied to the dialog container. |
| `AnimateHandoff` | `bool` | `false` | Morph into a predecessor that is still closing, instead of cross-fading. |
| `HandoffStyle` | `DrylViewTransitionStyle` | `DrylViewTransitionStyle.DepthGlass` | Morph tier for `AnimateHandoff`. Ignored while it is `false`. |

`AnimateHandoff` is the one option that is not about a single dialog. It
addresses the sequential pattern an agent produces — close A, immediately open
B — and is opt-in per call. Keep it consistent across every step of a chain: a
step that does not set it simply falls back to the cross-fade for that step.

## `DialogParameters`

The parameter bag forwarded to the dialog component. Keys are the component's
`[Parameter]` property names.

| Member | Purpose |
|---|---|
| `this[string name]` | Get or set a parameter. Setting `null` removes it. |
| `Add(string, object?)` | Add or replace a parameter; returns the bag, so calls chain. Adding `null` removes the key. |
| `Contains(string)` | Whether a parameter of that name is set. |
| `Count` | Number of parameters set. |
| `ToDictionary()` | Materialize for `DynamicComponent.Parameters`. |

`null` is treated as "not set" rather than as a value, on both the indexer and
`Add`. A dialog parameter that must be able to be `null` therefore carries its
own default on the component rather than relying on the bag to deliver one.

## `DialogResult`

The outcome of a dialog. A `sealed class` with a private constructor and static
factories — the two outcomes are constructed, never assembled field by field.

| Member | Purpose |
|---|---|
| `Canceled` | `true` when the dialog was dismissed without a confirmed outcome. |
| `Data` | Payload returned on confirmation; `null` when canceled. |
| `DialogResult.Ok()` | Confirmed, no payload. |
| `DialogResult.Ok(object?)` | Confirmed with a payload. |
| `DialogResult.Ok<T>(T)` | Confirmed with a typed payload. |
| `DialogResult.Cancel()` | Canceled. |
| `DataAs<T>()` | Read `Data` as `T`, or `default` when it is not a `T`. |

`DataAs<T>` never throws on a type mismatch. A dialog that returns the wrong
payload type yields `default`, which the caller can handle, rather than an
`InvalidCastException` from inside an awaited result.

## `IDrylDialogService`

The caller-facing API. Registered as **scoped** — one per Blazor circuit — by
`AddDrylComponents()`.

| Member | Purpose |
|---|---|
| `ShowAsync<TDialog>(string?, DialogParameters?, DialogOptions?)` | Show a dialog of type `TDialog`; returns its `IDrylDialogReference`. `TDialog` is constrained to `IComponent`. |
| `ShowConfirmAsync(string, string, string, string, DialogOptions?)` | Show a confirmation with a cancel and a confirm button; awaits and returns the `DialogResult`. |
| `ShowAlertAsync(string, string, string, DialogOptions?)` | Show an alert with a single button; awaits and returns the `DialogResult`. |
| `OnDialogInstanceAdded` | Raised when a dialog is requested. Subscribed by `DrylDialogProvider`. |
| `OnDialogCloseRequested` | Raised when a dialog closes, so the provider can retire it. |
| `OnDialogInstanceUpdated` | Raised when a dialog's own state changes and the provider must re-render. |

The three events are the seam between the service and the provider. They are
part of the public interface because the provider is an ordinary component that
subscribes to them, not because a consumer is expected to.

`ShowAsync` returns as soon as the dialog is requested; `ShowConfirmAsync` and
`ShowAlertAsync` return only when the user has answered. That difference is
deliberate: the generic call hands back a handle the caller may keep, close
programmatically or await later, while the two convenience calls exist precisely
to be awaited in one line.

`ShowConfirmAsync` and `ShowAlertAsync` default to `DialogSize.Small` when the
caller passes no options — a one-line question does not deserve a form-sized
frame. A supplied `DialogOptions` is taken as given, including its `Size`.

## `IDrylDialogReference`

The handle `ShowAsync` returns to the caller.

| Member | Purpose |
|---|---|
| `Id` | Stable id of this dialog instance. |
| `Result` | A `Task<DialogResult>` that completes when the dialog closes. |
| `Close(DialogResult)` | Close the dialog programmatically with a result. |
| `Cancel()` | Cancel the dialog programmatically. |

## `IDrylDialogInstance`

The same object, seen from inside the dialog component, cascaded by
`DrylDialogProvider`.

| Member | Purpose |
|---|---|
| `Id` | Stable id of this dialog instance. |
| `Title` | The title supplied at `ShowAsync` time. |
| `Options` | The `DialogOptions` the dialog was opened with. |
| `Ai` | The dialog frame's current AI state; starts at `DialogOptions.Ai`. |
| `Close(DialogResult)` | Close with a result. |
| `Cancel()` | Cancel — equivalent to closing with `DialogResult.Cancel()`. |
| `SetAi(AiState)` | Change the frame's AI state at runtime and re-render. |

One object implements both interfaces, so the caller's handle and the dialog's
instance are the same identity seen from two sides. `Close` is idempotent: the
first call completes `Result`, later ones are ignored, so a dialog that both
answers and is dismissed does not complete twice.

`SetAi` is what makes a dialog an AI surface for the length of an operation: the
dialog component calls it while it works, and the frame's aura follows without
the dialog re-rendering its own chrome.
