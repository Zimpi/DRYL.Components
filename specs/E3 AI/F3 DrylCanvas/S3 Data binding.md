# Data binding

## Meta
- **State:** Implemented

Nodes that read their values from host-registered data sources, and how those
values are refreshed and how the change is shown.

## Acceptance Criteria

### Binding

- A node binds to a host data source through its `data` object (`source`,
  `params`, `refresh`).
- The canvas resolves bindings only when the host registered at least one data
  source; without a registration the artifact still renders (`DrylCanvas` must
  work in an app that never called `AddDrylComponents`).
- Bindings that share a source and the same parameters are resolved once, not
  once per node.
- The refresh button appears in the header exactly when the artifact has at
  least one binding.
- A binding whose value has not yet arrived renders as a skeleton (see `S2`).

### Refresh

- Activating the refresh button refreshes every binding of the artifact.
- A binding declared with `interval:<n>s` refreshes on that interval.
- An interval below the binder's five-second floor is raised to it, so a spec
  cannot ask the host for a faster poll than the library is willing to run.
- A binding declared `manual`, or with an unparseable `refresh`, does not poll at
  all.
- One timer serves the whole canvas, not one per bound node.
- A binding refreshes when a form field it depends on changes.
- A binding refreshes when the host invalidates its source.
- At most one load runs per binding key; a newer load cancels the one in flight.
- The refresh button shows its loading state while any binding is loading.

### The change pulse

- A refresh that changes a value stamps the node into the pulse tracker.
- A refresh that changes nothing stamps nothing — the pulse marks change, not
  activity.
- A refresh never re-renders the node as a skeleton: the node keeps its identity
  and its previous value, and the pulse carries the movement instead. A skeleton
  is for a first load, where there is no value to keep.
- The host may supply its own `CanvasPulseTracker` through `Pulse`, so a patch
  author and the data binder stamp into the same tracker.
- Without a supplied tracker the canvas owns one, so pulses still work for a
  canvas nobody wraps.

### Lifecycle

- The binder is disposed with the component, and its change subscription is
  removed (`CODE-05`).
- Replacing `Spec` with a different instance resets the binder (see `S1`).
