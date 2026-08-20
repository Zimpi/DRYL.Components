# Feedback — Public API

Shared enums, parameter contracts and services of the Feedback category — the
part of the data contract the 1.0 freeze binds.

**Source folder:** `code/DRYL.Components/Components/Feedback/`

This file carries no `Meta` block: it is a reference for the specs around it,
not a unit of implementation (`SPEC-03`). The category holds eight components —
`DrylAlert`, `DrylTooltip`, `DrylSpinner`, `DrylSkeleton`, `DrylProgress`,
`DrylEmptyState`, `DrylErrorBoundary` and `DrylNotifications` — and unlike
`E4 Charts` they share almost nothing with each other. What they share is with
the rest of the library: seven of the eight take `Ai` and `Aura` and render the
one aura vocabulary specified in `E3 AI`.

So this file is mostly an inventory of per-component types. It is worth having
anyway, because the eight components disagree about **where a type is
declared**, and the 1.0 freeze binds that disagreement into the call sites of
every consumer.

## Where the enums live — and why it is not uniform

Three enums are declared at namespace level in their own file; five are nested
inside the component that uses them. The difference is visible to a consumer,
because a nested enum must be written qualified:

```razor
<DrylProgress Variant="ProgressVariant.Success" />
<DrylAlert    Kind="DrylAlert.AlertKind.Success" />
```

| Type | Declared in | Written as |
|---|---|---|
| `ProgressVariant` | `code/DRYL.Components/Components/Feedback/ProgressVariant.cs` | `ProgressVariant.Success` |
| `ProgressSize` | `code/DRYL.Components/Components/Feedback/ProgressVariant.cs` | `ProgressSize.Large` |
| `EmptyStateSize` | `code/DRYL.Components/Components/Feedback/EmptyStateSize.cs` | `EmptyStateSize.Small` |
| `DrylAlert.AlertKind` | `DrylAlert.razor` | `DrylAlert.AlertKind.Danger` |
| `DrylSpinner.SpinnerVariant` | `DrylSpinner.razor` | `DrylSpinner.SpinnerVariant.Dots` |
| `DrylSpinner.SpinnerSize` | `DrylSpinner.razor` | `DrylSpinner.SpinnerSize.Large` |
| `DrylSkeleton.SkeletonVariant` | `DrylSkeleton.razor` | `DrylSkeleton.SkeletonVariant.Card` |
| `DrylSkeleton.SkeletonSize` | `DrylSkeleton.razor` | `DrylSkeleton.SkeletonSize.Small` |
| `DrylTooltip.TooltipPlacement` | `DrylTooltip.razor` | `DrylTooltip.TooltipPlacement.Bottom` |

`ProgressSize` is the one that shows this is history rather than design: it sits
in `ProgressVariant.cs`, a file named after the *other* enum. Recorded as a
fact, not corrected — moving a public type between declaration sites is a source
break for every consumer that wrote it qualified, and the freeze is in force.

Two of the three namespace-level enums have nested twins with the same job:
`ProgressSize`, `EmptyStateSize`, `DrylSpinner.SpinnerSize` and
`DrylSkeleton.SkeletonSize` are four separate enums for what a reader would call
one concept. They are **not** interchangeable, and no component accepts another
component's size type.

## `DrylAlert.AlertKind`

Semantic variant of an alert. Nested in `DrylAlert`.

| Member | Notes |
|---|---|
| `Info` | The default of `DrylAlert.Kind`. Also the fallback for any unmatched value. |
| `Success` | |
| `Warning` | Announced assertively — see `F1`. |
| `Danger` | Announced assertively — see `F1`. Used by `DrylErrorBoundary` for its fallback surface. |
| `Ai` | Semantic AI provenance, chosen **independently** of the `Ai` parameter. |

`Kind` and `Ai` answer different questions and neither implies the other:
`Kind` says what the message *is*, `Ai` says whether something is *happening*.
A `Warning` alert can carry `AiState.Thinking` while its check is still running,
and an `AlertKind.Ai` alert can sit at `AiState.None` once its content is final.

## `ProgressVariant`

Color treatment of a `DrylProgress` fill. Namespace-level.

| Member | Notes |
|---|---|
| `Accent` | The default of `DrylProgress.Variant`. The accent gradient. |
| `Success` | The `--success` semantic. |
| `Warning` | The `--warning` semantic. |
| `Danger` | The `--danger` semantic. |

`Accent` is the only member that maps to no modifier class of its own — it is
the unmodified bar. The three others each add one.

## `ProgressSize`

Rendered thickness of a `DrylProgress` track. Namespace-level, declared in
`ProgressVariant.cs`.

| Member | Notes |
|---|---|
| `Small` | |
| `Medium` | The default of `DrylProgress.Size`. |
| `Large` | |

## `EmptyStateSize`

Overall size of a `DrylEmptyState`. Namespace-level. Two members, not three —
the odd one out among the size enums.

| Member | Notes |
|---|---|
| `Small` | Compact, for an empty state inside a panel or a dropdown. `DrylNotifications` uses it for its own empty inbox. |
| `Medium` | The default of `DrylEmptyState.Size`. Full-page or large-card. |

## `DrylSpinner.SpinnerVariant`

Visual style of a spinner. Nested in `DrylSpinner`.

| Member | Notes |
|---|---|
| `Ring` | The default of `DrylSpinner.Variant`. A rotating gradient arc. |
| `Dots` | Three dots in a sequential wave. The only variant whose host is a pill rather than a circle. |
| `Pulse` | Concentric rings expanding out of a core. |

## `DrylSpinner.SpinnerSize`

| Member | Notes |
|---|---|
| `Small` | |
| `Medium` | The default of `DrylSpinner.Size`. |
| `Large` | |

Each member sets the wrapper's size custom properties; every child dimension is
derived from them, so a size change never means editing a second value.

## `DrylSkeleton.SkeletonVariant`

Shape of a placeholder. Nested in `DrylSkeleton`.

| Member | Notes |
|---|---|
| `Line` | The default of `DrylSkeleton.Variant`. One bar, width overridable through `Width`. |
| `Text` | `Lines` stacked bars with varied widths. |
| `Avatar` | One circle. |
| `Card` | Composite: header avatar, two header lines, an image block, a body block. |
| `Image` | One wide rectangle. |
| `Custom` | Renders `ChildContent` instead of a built-in shape. |

`Custom` is the one member that makes the component's CSS classes part of its
public contract: a consumer's own layout uses `skel`, `skel-circle` and
`skel-rect` to get the shimmer, and those class names are therefore as frozen as
the parameters.

## `DrylSkeleton.SkeletonSize`

| Member | Notes |
|---|---|
| `Small` | |
| `Medium` | The default of `DrylSkeleton.Size`. |
| `Large` | |

## `DrylTooltip.TooltipPlacement`

Preferred side of the trigger. Nested in `DrylTooltip`.

| Member | Notes |
|---|---|
| `Top` | The default of `DrylTooltip.Placement`. |
| `Bottom` | |
| `Left` | |
| `Right` | |

A **preference**, not an instruction: the bubble flips to the opposite side when
the viewport has no room on the preferred one (`_Interop.md`).

## `DrylNotification`

One entry in the notification inbox. A `sealed class` — mutable, so `Read` can
be toggled in place. Identity is `Id`.

Declared in `code/DRYL.Components/Notifications/DrylNotification.cs`, outside the
category's source folder. `SPEC-02` derives a *component's* category from its
path; a supporting type has no such rule, and this one is specified here because
`DrylNotifications` is the only thing that consumes it.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Id` | `string` | a new GUID | Stable identity. `init`-only. Set it yourself to de-duplicate. |
| `Title` | `string` | `string.Empty` | Headline. |
| `Message` | `string?` | `null` | Supporting line under the title. |
| `Icon` | `string?` | `null` | `DrylIcon` name for the leading chip. |
| `Timestamp` | `DateTimeOffset` | `DateTimeOffset.Now` | Drives the relative "x ago" label. |
| `Read` | `bool` | `false` | Unread entries show the accent dot and count toward the badge. |
| `Ai` | `AiState` | `AiState.None` | AI provenance. Anything but `AiState.None` gives the row the shared aura. |

`Id` being `init`-only and defaulted is what makes `@key` stable across
re-renders; a caller who wants idempotent pushes supplies their own.

## `IDrylNotificationService`

The service-driven half of the inbox. Registered scoped by
`AddDrylComponents()` — see [`_Interop.md`](_Interop.md).

| Member | Signature | Purpose |
|---|---|---|
| `Notifications` | `IReadOnlyList<DrylNotification>` | Every entry, newest first. |
| `UnreadCount` | `int` | Entries whose `Read` is `false`. |
| `Add` | `DrylNotification Add(DrylNotification notification)` | Adds an entry at the front and returns it. |
| `Add` | `DrylNotification Add(string title, string? message = null, string? icon = null, AiState ai = AiState.None)` | Builds and adds an entry. |
| `MarkRead` | `void MarkRead(string id)` | Marks one entry read. |
| `MarkAllRead` | `void MarkAllRead()` | Marks every entry read. |
| `Remove` | `void Remove(string id)` | Removes one entry. |
| `Clear` | `void Clear()` | Removes every entry. |
| `OnChanged` | `event Action?` | Raised when the list changes, so a bound component re-renders. |

`OnChanged` is raised only on an **actual** change: marking an already-read entry
read, removing an unknown id or clearing an empty list raise nothing. That is
what keeps a bell in an app shell from re-rendering on every no-op call.

The default implementation is `internal`; a consumer replacing the service
registers their own `IDrylNotificationService` and never names the class.

## The AI parameters

Six of the eight components — `DrylAlert`, `DrylSpinner`, `DrylSkeleton`,
`DrylProgress`, `DrylEmptyState` and `DrylErrorBoundary` — carry the same two
parameters, with the same types and the same defaults:

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Ai` | `AiState` | `AiState.None` | Ambient AI state. AI styling is opt-in. |
| `Aura` | `AiAura?` | `null` | Pins the aura variant; `null` inherits a surrounding `DrylAiScope`. |

Both types belong to `E1 Foundation`; the aura vocabulary they drive is
specified in `E3 AI`. The two remaining components are the exceptions, each for
its own reason: `DrylNotifications` takes `Aura` but no `Ai`, because the state
belongs to each `DrylNotification` rather than to the inbox around them; and
`DrylTooltip` takes neither, because it renders no surface of its own to put an
aura on.

## `Class` and `AdditionalAttributes`

All eight components carry both:

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Class` | `string?` | `null` | Extra CSS class(es) **merged** onto the component's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

`Class` exists because a splatted `class` would otherwise clobber the
component's own classes. Blazor matches parameter names case-insensitively, so a
consumer writing `class="my-thing"` binds the typed `Class` parameter — not
`AdditionalAttributes` — and the classes merge. That is verified for two of the
eight in `tests/DRYL.Components.Tests/ClassMergeTests.cs`.
