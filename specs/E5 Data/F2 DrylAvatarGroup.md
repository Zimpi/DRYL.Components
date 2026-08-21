# DrylAvatarGroup

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylAvatarGroup.razor

## User Story

As a Blazor developer, I want to show that several people are attached to one
thing without spending a row of the layout on them, so that a shared document, a
conversation or a task can name its participants in the width of two or three
avatars.

## Description

`DrylAvatarGroup` is a container for `DrylAvatar` (see `F1`) that does two
things its members cannot do for themselves.

It **unifies the size**. Every avatar in the group renders at the group's
`Size`, whatever it was given individually, because a stack of avatars at
different diameters reads as a mistake rather than as a group.

It **caps the length**. With `Max` set, the avatars beyond the cap render
nothing at all and a counter tile takes their place, reading "+N". The cap is
counted over the avatars that actually registered, so a member removed by a
conditional disappears from the count as well as from the row.

The avatars overlap rather than sitting side by side, by a distance that scales
with the group's size.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Size` | `AvatarSize` | `AvatarSize.Medium` | Uniform size for every avatar in the group. |
| `Max` | `int?` | `null` | Number of avatars to show before collapsing the rest into the counter. `null` shows all. |
| `ChildContent` | `RenderFragment?` | `null` | The avatars. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the group's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the group element. |

The component takes **no** `Ai` and no `Aura` — see "AI mode" below.

## Acceptance Criteria

### Structure

- The component renders a single root element holding the avatars.
- The root carries the modifier class of its `Size`, one per value.
- `ChildContent` is rendered inside a cascading value that hands the group itself
  to its members.
- The cascade is fixed, so a member never re-subscribes to it.
- `Class` is merged onto the root's own classes rather than replacing them.
- `AdditionalAttributes` are applied to the root.
- The root is inline-flex, so a group sits inside a sentence or a toolbar rather
  than claiming a line.

### Membership

- An avatar registers itself with the group when it initialises.
- Registering an avatar that is already registered changes nothing, so a
  re-render cannot double-count a member.
- An avatar unregisters itself when it is disposed.
- Registering an avatar re-renders the group, so the counter reflects the new
  member.
- Unregistering an avatar re-renders the group, so the counter reflects its
  departure.
- Unregistering an avatar that is not a member re-renders nothing.

### The cap

- `Max` left `null` renders every avatar and no counter.
- `Max` at or above the number of registered avatars renders every avatar and no
  counter.
- `Max` below the number of registered avatars reports every avatar from that
  position onward as hidden.
- A hidden avatar renders nothing, so the row's width is the visible avatars
  plus the counter.
- Exceeding `Max` renders exactly one counter element, after the visible
  avatars.
- The counter reads the number of avatars it stands for.
- The counter carries the modifier class of the group's `Size`, so it matches
  the avatars beside it.
- The counter is not a `DrylAvatar` and carries no presence dot.

### Keyboard and accessibility

- The counter carries `role="img"`, so it is announced as one thing rather than
  as a stray "+3".
- The counter carries an accessible label naming how many further avatars it
  stands for.
- Neither the group nor the counter is focusable, and neither adds a stop to the
  tab order, because the group is a representation and not a control.
- The group applies no role of its own, so it does not claim to be a list it
  does not behave like.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The counter is filled with `--glass-3` and its text is drawn in `--fg-muted`,
  so it reads as the quiet member of the row rather than as another identity.
- Each avatar overlaps the one before it, by a distance that differs per `Size`.
- The group paints no surface of its own — no fill, no border, no frost — so it
  inherits whatever ground it is placed on and `DESIGN-06` has nothing to apply
  to.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### AI mode

- The component takes no `Ai` parameter and renders no aura.
- The decision follows `F1` for the same reason (`AI-05`): the group states *who*
  is attached to something. If one of those participants is an assistant, that is
  a property of the avatar's identity, not an activity signal, and the activity
  belongs to the surface the group sits on.

## Recorded gaps

- **Registration order is not document order after the first change.** Members
  are appended in the order they initialise and removed by identity, so an avatar
  added later always lands at the end of the list. Remove the first participant
  and add another, and the cap now hides an avatar that is not the last one in
  the markup — the hidden member and the "+N" tile disagree with what the reader
  sees.
- **A negative `Max` hides everything and over-counts.** Nothing constrains
  `Max` to be non-negative. At `-1` every avatar reports as hidden and the
  counter reads one *more* than the number of avatars in the group.
- **Every member costs the group a render.** Registration calls
  `StateHasChanged` unconditionally, so mounting a group of *n* avatars queues
  *n* extra renders of the group and its whole subtree during the first render
  pass. The re-render is only needed when the counter's value actually changes.
- **The overlap distances are literals.** `-6px`, `-8px` and `-12px` are written
  into the `.avatar-group` rules in `dryl.css` with no token behind them
  (`DESIGN-01`), and they are the third place — after the avatar's own sizes and
  the component's icon sizes — where the size scale is restated by hand.
- **Nothing is animated.** An avatar joining or leaving the group appears and
  disappears instantly, and the counter's number changes without any transition
  (`DESIGN-11`, `DESIGN-12`). The group is exactly the component that would
  benefit — membership is the thing that changes at runtime.
- **No tests of its own.** None of the criteria above is guarded by a test,
  including the cap, which is the component's only real logic.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--glass-3` and `--fg-muted` are
  the mode-dependent tokens; the component defines no mode-specific rule.
- **Enter/exit animation** — **absent**, and recorded above as debt rather than
  as an exception.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is that the counter announces what it stands for rather
  than being read as a piece of punctuation.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — shown on `DRYL.Website/Components/Pages/DemoAvatar.razor`
  through the example `Components/Examples/Avatar/Group.razor`; the component
  has no page of its own, being usable only around `DrylAvatar`.
- **`ComponentCatalog`** — reached through the `"Avatar"` / `avatar` entry in
  `DRYL.Website/Components/ComponentCatalog.cs`. The catalog registers the lead
  component of a family and not its parts, which is why the group, like every
  other member component in this category, carries no entry of its own.
