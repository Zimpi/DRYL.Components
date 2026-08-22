# Data — Public API

Shared enums, parameter contracts and services of the Data category — the part
of the data contract the 1.0 freeze binds.

**Source folder:** `code/DRYL.Components/Components/Data/`

This file carries no `Meta` block: it is a reference for the specs around it,
not a unit of implementation (`SPEC-03`).

The category holds twenty-one components and is the library's most varied. It is
better read as five unrelated groups than as one family:

| Group | Components |
|---|---|
| Inline marks | `DrylBadge`, `DrylIcon`, `DrylKbd` |
| Identity | `DrylAvatar`, `DrylAvatarGroup` |
| Records | `DrylDescriptionList`, `DrylDescriptionItem`, `DrylTimeline`, `DrylTimelineItem`, `DrylTreeView`, `DrylTreeNode`, `DrylCitation`, `DrylCitationList`, `DrylCitationListItem` |
| Numbers | `DrylStat`, `DrylTableKpi`, `DrylSparkline` |
| The table | `DrylTable`, and its `DrylColumn` plus the types under `Models/` |
| Media | `DrylImage`, `DrylCodeBlock` |

`DrylSparkline` sits in this category rather than in `E4 Charts` because it
lives in `Components/Data/` and not in `Components/Data/Charts/`; `SPEC-02`
derives a component's category from its path.

## Where the enums live — and why it is not uniform

The category declares nineteen public types. Eight are namespace-level enums in
their own file; six are nested inside the component that uses them; and five are
records or enums under `Models/` belonging to the table. A nested enum must be
written qualified at the call site, and the 1.0 freeze binds that difference
into every consumer's markup:

```razor
<DrylAvatar Size="AvatarSize.Large" />
<DrylBadge  Kind="DrylBadge.BadgeKind.Success" />
<DrylImage  Ratio="DrylImage.ImageRatio.Wide" />
```

| Type | Declared in | Written as |
|---|---|---|
| `AvatarShape` | `AvatarShape.cs` | `AvatarShape.Square` |
| `AvatarSize` | `AvatarSize.cs` | `AvatarSize.Large` |
| `AvatarStatus` | `AvatarStatus.cs` | `AvatarStatus.Online` |
| `DeltaDirection` | `DeltaDirection.cs` | `DeltaDirection.Up` |
| `DescriptionLayout` | `DescriptionLayout.cs` | `DescriptionLayout.Inline` |
| `SparklineKind` | `SparklineKind.cs` | `SparklineKind.Area` |
| `TimelineVariant` | `TimelineVariant.cs` | `TimelineVariant.Success` |
| `ColumnAlign` | `Models/ColumnAlign.cs` | `ColumnAlign.End` |
| `ColumnFilterType` | `Models/ColumnFilterType.cs` | `ColumnFilterType.Select` |
| `ColumnPin` | `Models/ColumnPin.cs` | `ColumnPin.Start` |
| `FilterOperator` | `Models/FilterDescriptor.cs` | `FilterOperator.In` |
| `SortDirection` | `Models/SortDescriptor.cs` | `SortDirection.Descending` |
| `TableEditMode` | `Models/TableEditMode.cs` | `TableEditMode.Cell` |
| `DrylBadge.BadgeKind` | `DrylBadge.razor` | `DrylBadge.BadgeKind.Danger` |
| `DrylImage.ImageFit` | `DrylImage.razor` | `DrylImage.ImageFit.Contain` |
| `DrylImage.ImagePosition` | `DrylImage.razor` | `DrylImage.ImagePosition.Top` |
| `DrylImage.ImageRounded` | `DrylImage.razor` | `DrylImage.ImageRounded.Full` |
| `DrylImage.ImageRatio` | `DrylImage.razor` | `DrylImage.ImageRatio.Square` |
| `DrylTableKpi.KpiDeltaKind` | `DrylTableKpi.razor` | `DrylTableKpi.KpiDeltaKind.Positive` |
| `DrylTableKpi.KpiTrend` | `DrylTableKpi.razor` | `DrylTableKpi.KpiTrend.Up` |

Two enums live in a file named after another type — `FilterOperator` in
`FilterDescriptor.cs` and `SortDirection` in `SortDescriptor.cs`. Recorded as a
fact, not corrected: the declaring *file* is not part of a C# type's identity,
so unlike a move between namespaces or nesting levels this costs a consumer
nothing.

## The duplicated vocabularies

Three concepts are expressed twice in this category, by types that are not
convertible to each other. All six are frozen, so this is an inventory rather
than a plan.

| Concept | Expressed as | And also as |
|---|---|---|
| A trend and its colour | `DeltaDirection` (`DrylStat`) | `DrylTableKpi.KpiDeltaKind` **and** `DrylTableKpi.KpiTrend` |
| A tiny trend chart | `DrylSparkline` with `SparklineKind` | `DrylTableKpi`'s inline chart, with no parameter |
| A size scale | `AvatarSize` | `DrylIcon.Size`, an `int` of pixels |

`DeltaDirection` folds direction and colour into one value; `DrylTableKpi` keeps
them apart, which is the one thing the duplicate does better — a falling latency
wants a downward arrow in the positive colour, and `DrylStat` cannot express
that. See `F17` for the rest of that component's recorded debt.

## `AvatarShape`

| Member | Notes |
|---|---|
| `Circle` | The default of `DrylAvatar.Shape`. The unmodified avatar. |
| `Square` | Rounded square, from `--r-sm`. For entities that are not people. |

## `AvatarSize`

| Member | Notes |
|---|---|
| `Small` | Dense lists, inline mentions. |
| `Medium` | The default of `DrylAvatar.Size` and of `DrylAvatarGroup.Size`. The unmodified avatar. |
| `Large` | Headers, profile cards, chat messages. |

A `DrylAvatarGroup` overrides the `Size` of every avatar inside it.

## `AvatarStatus`

| Member | Notes |
|---|---|
| `None` | The default of `DrylAvatar.Status`. No dot, and no wrapper element either — the markup changes with this value. |
| `Online` | `--success`, with a glow. |
| `Busy` | `--danger`, with a glow. |
| `Away` | `--warning`, with a glow. |
| `Offline` | `--fg-faint`, deliberately without one. |

## `DeltaDirection`

| Member | Notes |
|---|---|
| `None` | The default of `DrylStat.Direction`. Suppresses the delta chip entirely, whatever `Delta` says. |
| `Up` | `--success`, with an upward arrow. |
| `Down` | `--danger`, with a downward arrow. |
| `Neutral` | `--fg-dim`, with no arrow. |

## `DescriptionLayout`

| Member | Notes |
|---|---|
| `Stacked` | The default of `DrylDescriptionList.Layout`. Term above value. Also what an item outside a list falls back to. |
| `Inline` | Term and value on one row, with a fixed label column. |

## `SparklineKind`

| Member | Notes |
|---|---|
| `Line` | The default of `DrylSparkline.Kind`. |
| `Area` | The line, plus a filled area beneath it. |
| `Bar` | One bar per point; the one kind that ignores `ShowLastDot`. |

## `TimelineVariant`

| Member | Notes |
|---|---|
| `Default` | The default of `DrylTimelineItem.Variant`, and the fallback for any unmatched value. Neutral glass marker. |
| `Accent` | `--accent-a` on an `--accent-line` border. |
| `Success` | `--success`. |
| `Warning` | `--warning`. |
| `Danger` | `--danger`. |

Independent of the item's `Ai`: a step can be semantically successful and
currently re-running.

## `DrylBadge.BadgeKind`

| Member | Notes |
|---|---|
| `Neutral` | The default of `DrylBadge.Kind`, and the fallback for any unmatched value. The unmodified pill. |
| `Accent` | `--accent-fg` on `--accent-soft`. |
| `Success` | `--success`. |
| `Warning` | `--warning`. |
| `Danger` | `--danger`. |

## `DrylImage`'s four enums

| Type | Members | Notes |
|---|---|---|
| `ImageFit` | `Cover`, `Contain`, `Fill`, `None`, `ScaleDown` | `Cover` is the default and the fallback for any unmatched value. |
| `ImagePosition` | `Center`, `Top`, `Bottom`, `Left`, `Right` | `Center` is the default and the fallback. |
| `ImageRounded` | `None`, `Sm`, `Md`, `Lg`, `Full` | Maps onto the radius scale; `Full` maps to `--r-pill`. |
| `ImageRatio` | `Auto`, `Square`, `Video`, `Portrait`, `Wide` | `Auto` derives the ratio from `Width` and `Height`; any other value overrides them. |

## `DrylTableKpi.KpiDeltaKind` and `DrylTableKpi.KpiTrend`

| Type | Members | Notes |
|---|---|---|
| `KpiDeltaKind` | `Positive`, `Negative`, `Neutral` | `Neutral` is the default. Colour only. |
| `KpiTrend` | `Up`, `Down`, `None` | `None` is the default. Arrow only. |

Deliberately independent of each other — see the duplicated-vocabularies note
above.

## The table's types

### `DrylColumn<TItem>`

The declarative column. Placed in `DrylTable`'s `Columns` slot, it registers
itself through a cascade and renders nothing of its own.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Field` | `Expression<Func<TItem, object?>>?` | `null` | Value, default sort key and default search source. |
| `Title` | `string?` | `null` | Header text. |
| `ColumnKey` | `string?` | `null` | Stable key. Derived from `Field`'s member name when unset. |
| `Sortable` | `bool` | `false` | Click-to-sort on this column's header. |
| `Searchable` | `bool` | `false` | Includes this column in the toolbar search. |
| `Filterable` | `bool` | `false` | Renders a filter control in the header. |
| `FilterType` | `ColumnFilterType` | `ColumnFilterType.Auto` | Kind of filter UI. |
| `FilterValues` | `Func<IEnumerable<TItem>, IEnumerable<object?>>?` | `null` | Overrides the derived option set of a select filter. |
| `Primary` | `bool` | `false` | Marks the identity column; its cells get the primary emphasis. |
| `Align` | `ColumnAlign` | `ColumnAlign.Start` | Horizontal alignment of header and cells. |
| `Width` | `string?` | `null` | Explicit width, any CSS length. |
| `Pinned` | `ColumnPin` | `ColumnPin.None` | Freezes the column to an edge. Excludes it from drag reorder. |
| `Resizable` | `bool` | `true` | Whether `ResizableColumns` applies to this column. |
| `Reorderable` | `bool` | `true` | Whether `ReorderableColumns` applies to this column. |
| `Hidden` | `bool` | `false` | Starts hidden; the visibility menu can show it. |
| `CellTemplate` | `RenderFragment<TItem>?` | `null` | Cell renderer. |
| `EditTemplate` | `RenderFragment<TItem>?` | `null` | Editor for inline editing. Its presence is what makes the column editable. |
| `HeaderTemplate` | `RenderFragment?` | `null` | Header renderer. |

Read-only members a consumer may use:

| Member | Type | Purpose |
|---|---|---|
| `Key` | `string` | The resolved stable key. |
| `DisplayTitle` | `string` | `Title`, falling back to `Key`. |
| `FieldGetter` | `Func<TItem, object?>?` | The compiled accessor. |
| `FieldType` | `Type?` | The field expression's CLR type. |
| `ResolvedFilterType` | `ColumnFilterType` | `FilterType` with `Auto` resolved against `FieldType`. |
| `IsEditable` | `bool` | Whether an `EditTemplate` was supplied. |
| `TextAlign` | `string` | `Align` as a CSS alignment value. |

Either `Field` or a `CellTemplate` is required. `Auto` resolves to
`ColumnFilterType.Select` for an enum, a `bool` or a nullable `bool`, and to
`ColumnFilterType.Text` for everything else.

### `ColumnAlign`

| Member | Notes |
|---|---|
| `Start` | The default. Adds no modifier class and no inline alignment. |
| `Center` | |
| `End` | |

### `ColumnFilterType`

| Member | Notes |
|---|---|
| `Auto` | The default of `DrylColumn.FilterType`. Resolved from the field's type. |
| `Text` | A contains filter over the value's text. |
| `Select` | A checkbox list over the distinct values. |

### `ColumnPin`

| Member | Notes |
|---|---|
| `None` | The default. The only value that can be reordered. |
| `Start` | Frozen to the leading edge; rendered before every unpinned column. |
| `End` | Frozen to the trailing edge; rendered after every unpinned column. |

### `TableEditMode`

| Member | Notes |
|---|---|
| `Row` | The default of `DrylTable.EditMode`. Every editable column of the row enters its editor. |
| `Cell` | Only the activated column does. |

### `SortDirection` and `SortDescriptor`

`SortDirection` is `Ascending` or `Descending`; there is no "none" member —
absence from the sort list is what "not sorted" means.

`SortDescriptor` is a `sealed record` of a `ColumnKey` and a `Direction`.
Several combine into a multi-sort, applied in list order, first primary.

### `FilterOperator` and `FilterDescriptor`

`FilterDescriptor` is a `sealed record` of a `ColumnKey`, an `Operator` and a
`Value`. Several are AND-combined.

`FilterOperator` has ten members: `Contains`, `Equals`, `NotEquals`,
`GreaterThan`, `LessThan`, `GreaterThanOrEqual`, `LessThanOrEqual`, `In`,
`IsNull`, `IsNotNull`.

**The table's own pipeline implements two of them.** The filter UI emits only
`Contains` and `In`, and the client-side matcher understands only those two — a
descriptor carrying any other operator is currently ignored by the client
pipeline rather than applied. The remaining eight exist for a `DataProvider`,
which receives whatever descriptors it is given and is free to translate all
ten. A consumer constructing descriptors by hand for a client-side table should
know that eight of the ten are inert.

### `DataRequest` and `DataResult<TItem>`

The server-mode contract, both `sealed record`s.

| `DataRequest` | Type | Purpose |
|---|---|---|
| `Skip` | `int` | Rows to skip; `0` when paging is off. |
| `Take` | `int` | Rows to take; `int.MaxValue` when paging is off, and for an export. |
| `SearchText` | `string?` | The applied search, or `null`. |
| `Sort` | `IReadOnlyList<SortDescriptor>` | Sort in priority order. |
| `Filters` | `IReadOnlyList<FilterDescriptor>` | Active filters. |

| `DataResult<TItem>` | Type | Purpose |
|---|---|---|
| `Items` | `IReadOnlyList<TItem>` | The rows for this request. |
| `TotalCount` | `int` | Rows matching the search and filters across all pages. |

### `RowEditEventArgs<TItem>` and `RowReorderEventArgs`

| Type | Shape | Purpose |
|---|---|---|
| `RowEditEventArgs<TItem>` | `(TItem Item, TItem EditedItem)` | Raised on commit. `Item` is the original; `EditedItem` is the working copy, or the same instance when no `CloneRow` was supplied. |
| `RowReorderEventArgs` | `(int OldIndex, int NewIndex)` | Raised after a row move. Indices into the displayed view. |

## The AI parameters

Five of the twenty-one components — `DrylCodeBlock`, `DrylImage`, `DrylStat`,
`DrylTable` and `DrylTimelineItem` — carry the same two parameters, with the
same types and the same defaults:

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Ai` | `AiState` | `AiState.None` | Ambient AI state. AI styling is opt-in. |
| `Aura` | `AiAura?` | `null` | Pins the aura variant; `null` inherits a surrounding `DrylAiScope`. |

Both types belong to `E1 Foundation`; the aura vocabulary they drive is
specified in `E3 AI`.

The sixteen that do not carry them are not oversights: every one of their specs
records the decision and its reason under "AI mode" (`AI-05`). Three reasons
recur. A component that **paints no surface** has nothing to put an aura on —
`DrylIcon`, `DrylKbd`, `DrylDescriptionList`, `DrylTimeline`, `DrylTreeView`. A
component that **states an identity or a classification** rather than an
activity would contradict itself with one — `DrylAvatar`, `DrylBadge`, the whole
citation family. And a component that is **a mark inside another surface** would
compete with that surface's own state — `DrylSparkline`, `DrylTableKpi`.

Two of these are worth reading in full because the "no" is the interesting
answer: `F4 DrylCitation`, where the component most obviously about AI must not
carry the AI vocabulary, and `F20`/`F21`, where the tree family's "no" is
recorded as a present-tense fact rather than a principle.

Three components render the aura in three different ways, which is worth knowing
before changing any of them:

| Component | How |
|---|---|
| `DrylStat`, `DrylTimelineItem`, `DrylCodeBlock`, `DrylTable` | The shared helper writes both the classes and the layers. |
| `DrylImage` | Writes the layers by hand, because it adds effects to the image itself on top of them. |
| `DrylTable` | Uses the shared helper, but only when `Bordered` — a borderless table has no box for the ring to trace. |

## `Class` and `AdditionalAttributes`

Nineteen of the twenty-one components carry both:

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Class` | `string?` | `null` | Extra CSS class(es) **merged** onto the component's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

`Class` exists because a splatted `class` would otherwise clobber the
component's own classes. Blazor matches parameter names case-insensitively, so a
consumer writing `class="my-thing"` binds the typed `Class` parameter — not
`AdditionalAttributes` — and the classes merge.

**Two components carry neither: `DrylDescriptionItem` and `DrylTreeNode`.** Both
are member components of a family, and in both cases it means a consumer cannot
attach a class, a `data-*` attribute or a test hook to a single field or a
single node. They are the category's outstanding half of the library-wide
rollout.

`DrylAvatar` carries both and applies them to the wrong element when `Status` is
set: the root is then a wrapper, and both land on the avatar inside it. See
`F1`.

Of the nineteen, three are guarded in
`tests/DRYL.Components.Tests/ClassMergeTests.cs`: `DrylBadge`, `DrylStat` and
`DrylTimeline`. The other sixteen — including `DrylTable`, where a clobbered
class would take the whole card surface with it — are unguarded.
