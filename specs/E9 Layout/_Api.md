# Layout — Public API

Shared enums, parameter contracts and services of the Layout category — the
part of the data contract the 1.0 freeze binds.

**Source folder:** `code/DRYL.Components/Components/Layout/`

This reference lists the types more than one component reads, and the
top-level enums of the category. It has no `Meta` block and claims no
component (`SPEC-03`). Enums nested inside a single component
(`DrylAspectRatio.AspectRatio`, `DrylContainer.ContainerSize`,
`DrylDivider.DividerOrientation`, `DrylGrid.ItemWidth`, `DrylList.ListVariant`,
`DrylList.ListDensity`, `DrylTypo.TypoVariant`, `DrylTypo.TypoColor`,
`DrylTypo.TypoAlign`) are specified with their component; consumer code names
them qualified (`DrylGrid.ItemWidth.Sm`). The `DrylStack` enums are listed
here because `DrylGrid` reads one of them.

## `Breakpoint`

Defined in `code/DRYL.Components/Components/Layout/Breakpoint.cs`, namespace
`DRYL.Components`. The fixed breakpoint scale; the widths themselves live only
in `dryl.css`, because a media or container query condition cannot read a
custom property.

| Member | Meaning |
|---|---|
| `Sm` | Phone landscape and small slots. |
| `Md` | Tablet. |
| `Lg` | Desktop. |
| `Xl` | Large desktop. |

The members grow strictly in the order `Sm` < `Md` < `Lg` < `Xl`. Consumers:
`DrylStack.CollapseBelow`, `DrylTable.CompactBelow` (E5) and
`DrylCalendar.CollapseBelow` (E5) take it as a typed parameter; `DrylGrid`'s
fixed-column step-down uses the `Md` and `Sm` widths as container queries; the
app shell (`DrylLayout`, `DrylAppBar`, `DrylDrawer`) switches between desktop
and overlay at the `Lg` width as a viewport query.

## `AppBarElevation`

Defined in `code/DRYL.Components/Components/Layout/AppBarElevation.cs`,
namespace `DRYL.Components`; claimed by
[`F1 DrylAppBar.md`](F1%20DrylAppBar.md).

| Member | Meaning |
|---|---|
| `Flat` | Default. Flush with the content: `--bar-bg` fill and a `--line` bottom border. |
| `Raised` | Lifted off the content: `--glass-2` fill and `--shadow-md`. |

## `SidebarMode`

Defined in `code/DRYL.Components/Components/Layout/SidebarMode.cs`, namespace
`DRYL.Components`; claimed and specified by
[`F18 DrylDrawer.md`](F18%20DrylDrawer.md).

| Member | Meaning |
|---|---|
| `Auto` | Default. In-flow column at desktop width, overlay below it. |
| `Static` | Always an in-flow column; never dismissable. |
| `Collapsible` | In-flow column the user can fold to an icon rail. |
| `Pinnable` | Like `Collapsible`, with the collapsed state persisted. |
| `Flyout` | An overlay at every width, with backdrop, focus trap and Escape. |

## `DrylLayoutContext`

Defined in `code/DRYL.Components/Components/Layout/DrylLayoutContext.cs`,
namespace `DRYL.Components`. A sealed class; one instance is created by each
`DrylLayout` and cascaded (`IsFixed="true"`) to its descendants. It is how the
app shell's parts coordinate without consumer wiring: `DrylDrawer` registers
itself and, when collapsible, its collapse toggle; `DrylAppBar` reads the
state to render its toggles; `DrylLayout` reads `SidebarCollapsed` to reflow
its grid. A consumer may also take it as a `[CascadingParameter]` to drive
the drawer from its own controls.

| Member | Kind | Meaning |
|---|---|---|
| `HasDrawer` | `bool` property | `true` while a `DrylDrawer` is registered. |
| `DrawerOpen` | `bool` property | The registered drawer's overlay open state; `false` without a drawer. |
| `SidebarCollapsed` | `bool` property, public get | The registered collapsible drawer's collapsed state; `false` without one. Set by `DrylDrawer` only. |
| `CanCollapseSidebar` | `bool` property | `true` while a collapsible or pinnable drawer has registered its toggle. |
| `StateChanged` | `event Action?` | Raised on every registration, unregistration, open-state and collapse change. |
| `ToggleDrawerAsync()` | `Task` method | Toggles the registered drawer's open state; a no-op without one. |
| `OpenDrawerAsync()` | `Task` method | Opens the registered drawer; a no-op without one. |
| `CloseDrawerAsync()` | `Task` method | Closes the registered drawer; a no-op without one. |
| `ToggleSidebarAsync()` | `Task` method | Collapses or expands the registered collapsible drawer; a no-op without one. |

Registration (`RegisterDrawer`, `UnregisterDrawer`, `RegisterSidebarToggle`,
`UnregisterSidebarToggle`, `NotifyStateChanged`) is `internal`. One drawer is
registered at a time; a second registration replaces the first. Unregistering
only takes effect for the drawer or toggle that is currently registered.
Unregistering the collapse toggle resets `SidebarCollapsed` to `false`.
`StateChanged` is raised synchronously on the thread that changed the state;
subscribers marshal with `InvokeAsync`.

## `DrylStack` enums

Nested in `DrylStack` (`code/DRYL.Components/Components/Layout/DrylStack.razor`),
specified with that component. `StackGap` is the spacing enum shared by the
layout primitives: `DrylStack.Gap`, `DrylGrid.Gap` and `DrylSpacer.Size` take
it, and all three map it onto the `--sp-*` scale identically — `DrylGrid` and
`DrylSpacer` through the internal `DrylGrid.GapToken`, `DrylStack` through a
copy of the same mapping in its own code.

| Enum | Members |
|---|---|
| `DrylStack.StackGap` | `None`, `Xs`, `Sm`, `Md`, `Lg`, `Xl`, `Xxl` |
| `DrylStack.StackDirection` | `Vertical`, `Horizontal` |
| `DrylStack.StackAlign` | `Stretch`, `Start`, `Center`, `End`, `Baseline` |
| `DrylStack.StackJustify` | `Start`, `Center`, `End`, `Between`, `Around`, `Evenly` |

`StackGap` maps `None` to a zero gap, `Xs` to `--sp-1`, `Sm` to `--sp-2`, `Md`
to `--sp-3`, `Lg` to `--sp-4`, `Xl` to `--sp-5` and `Xxl` to `--sp-6`.

## `RevealTransition`

Declared in `code/DRYL.Components/RevealTransition.cs` and owned by
[`F10 DrylReveal`](F10%20DrylReveal.md). Members, in this order: `Fade`,
`Rise`, `ScaleIn`.

## Class and attribute contract

`DrylAppBar`, `DrylAspectRatio`, `DrylContainer`, `DrylDivider`, `DrylGrid`,
`DrylLayout` and `DrylList` each expose `Class` and capture unmatched
attributes into `AdditionalAttributes`. `Class` is merged into the
component's own class list, and a `class` passed as an unmatched attribute
binds to `Class` (parameter names match case-insensitively), so it merges
instead of clobbering. A `style` passed as an unmatched attribute is splatted
after the component's own computed `style` and replaces it — recorded as debt
in each spec whose layout lives in that inline style.
