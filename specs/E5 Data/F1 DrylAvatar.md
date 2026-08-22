# DrylAvatar

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylAvatar.razor
              code/DRYL.Components/Components/Data/AvatarShape.cs
              code/DRYL.Components/Components/Data/AvatarSize.cs
              code/DRYL.Components/Components/Data/AvatarStatus.cs

## User Story

As a Blazor developer, I want a small tile that stands for a person or an entity
and that always shows *something* — a photo, their initials, or an icon — so that
a row, a mention or a chat message never renders an empty hole when the picture
is missing or fails to load.

## Description

`DrylAvatar` resolves a face through a fixed chain and stops at the first link
that can be satisfied: the image, then initials, then the named icon, then a
generic user icon. The chain has no hole in it, which is the component's whole
point — a consumer can bind it to whatever their data happens to carry and never
has to write the fallback themselves.

Initials are derived rather than demanded. Given a `Name`, the component takes
the first letter of the first word and the first letter of the last word;
`Initials` overrides that when the derivation would be wrong.

Two optional extras sit on top of the face. `Status` adds a presence dot, which
also changes the markup: the avatar is wrapped so the dot has something to be
positioned against. `Shape` turns the circle into a rounded square, for entities
that are not people.

Placed inside a `DrylAvatarGroup` (see `F2`) the avatar hands itself to that
group and takes its size from it, and may be collapsed behind the group's "+N"
counter entirely.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Src` | `string?` | `null` | Image URL. A load error falls through to the next link in the chain. |
| `Alt` | `string?` | `null` | Alternative text for the image, and the first choice of accessible label. |
| `Name` | `string?` | `null` | Full name; initials are derived from it. |
| `Initials` | `string?` | `null` | Explicit initials, overriding the derivation. |
| `Icon` | `string?` | `null` | `DrylIcon` name for the icon link of the chain. |
| `Size` | `AvatarSize` | `AvatarSize.Medium` | Rendered diameter. Overridden by a surrounding `DrylAvatarGroup`. |
| `Shape` | `AvatarShape` | `AvatarShape.Circle` | Outline shape. |
| `Status` | `AvatarStatus` | `AvatarStatus.None` | Presence dot. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the avatar's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the avatar element. |

The component takes **no** `Ai` and no `Aura` — see "AI mode" below.

## Acceptance Criteria

### The fallback chain

- `Src` set and loading renders an `img` element inside the avatar.
- `Src` set but failing to load falls through to the next satisfiable link of the
  chain, so a broken URL never leaves an empty tile.
- `Src` unset renders the initials when initials can be derived.
- `Initials` set wins over any value derived from `Name`.
- `Initials` is rendered upper-cased, so mixed-case input still reads as a
  monogram.
- `Initials` is rendered trimmed, so stray whitespace does not shift it off
  centre.
- `Name` holding a single word yields its first letter.
- `Name` holding two or more words yields the first letter of the first word and
  the first letter of the last word.
- `Name` holding only whitespace yields no initials, so the chain falls through
  rather than rendering a blank monogram.
- Neither an image nor initials renders `Icon` as a `DrylIcon`.
- Neither an image, nor initials, nor `Icon` renders the `User` icon, so the
  component always renders a face.

### Structure

- `Status` at `AvatarStatus.None` renders the avatar element as the root, with no
  wrapper around it.
- `Status` at any other value renders a wrapper element holding the avatar and
  one dot element.
- The dot carries the modifier class of its `AvatarStatus` value, one per value.
- The wrapper carries the modifier class of the effective size, so the dot scales
  with the avatar.
- The avatar element carries the modifier class of the effective size.
- The avatar element carries the modifier class of `AvatarShape.Square`;
  `AvatarShape.Circle` is the unmodified element.
- `Class` is merged onto the avatar element's own classes rather than replacing
  them.
- `AdditionalAttributes` are applied to the avatar element.
- The avatar clips its content, so an image with the wrong aspect ratio cannot
  escape the shape.
- The image fills the avatar and is cropped to cover it, so a non-square source
  is not distorted.
- The avatar does not shrink when placed in a flex row that runs out of space.

### Inside a group

- An avatar inside a `DrylAvatarGroup` registers itself with that group on
  initialisation.
- An avatar inside a `DrylAvatarGroup` unregisters itself when it is disposed, so
  a removed member stops counting toward the group's overflow.
- An avatar inside a `DrylAvatarGroup` renders at the group's `Size` and ignores
  its own.
- An avatar the group reports as hidden renders nothing at all.
- An avatar outside a group renders at its own `Size`.

### Keyboard and accessibility

- The avatar element carries `role="img"`, so it is announced as one thing rather
  than as its letters.
- `Alt` set is the accessible label.
- `Alt` unset and `Name` set makes `Name` the accessible label, so an
  initials-only avatar still announces the person.
- Neither set yields a generic label rather than nothing.
- The avatar is not focusable and adds no stop to the tab order, because it is a
  representation and not a control.
- The presence dot is hidden from assistive technology.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The face without an image is filled with `--accent-grad` and its letters are
  drawn in `--on-accent`.
- The letters are set in `--font-mono`, so two-letter monograms of different
  widths still centre alike.
- `AvatarStatus.Online` draws its dot from `--success`.
- `AvatarStatus.Busy` draws its dot from `--danger`.
- `AvatarStatus.Away` draws its dot from `--warning`.
- `AvatarStatus.Offline` draws its dot from `--fg-faint`.
- The three live statuses carry a glow of their own color; `Offline` deliberately
  does not, so absence is the one state that does not attract the eye.
- `AvatarShape.Square` takes its corner from `--r-sm` rather than from a written
  radius.
- The avatar paints no frost, being a small opaque tile rather than a floating
  surface (`DESIGN-06`).
- The accent appears as a small tile no larger than a line of text, not as the
  fill of a large surface (`DESIGN-08`).
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### AI mode

- The component takes no `Ai` parameter and renders no aura.
- The decision is deliberate (`AI-05`): an avatar states *who* something is, and
  the AI vocabulary states *what is happening*. Those are different questions,
  and the answer to the second belongs to the thing the avatar is attached to —
  the message, the row, the timeline item — each of which carries its own `Ai`.
  An assistant's avatar is identified by its `Icon` and its `Name`, not by an
  aura that would then contradict the state of the message beside it.

## Recorded gaps

- **A failed image is remembered forever.** The load error sets an internal flag
  that nothing resets, so assigning a new, working `Src` to an avatar whose
  previous URL failed keeps showing the fallback for the lifetime of that
  component instance. A list that reuses avatar instances across rows —
  everything Blazor does without an explicit `@key` — can therefore show the
  wrong person's initials.
- **The presence dot is silent.** `Status` is announced to nobody: the dot is
  `aria-hidden` and the accessible label is built from `Alt` and `Name` alone. A
  screen-reader user cannot tell an online colleague from an offline one, which
  is the only information the dot exists to carry.
- **`Class` lands inside the wrapper, not on the root.** With `Status` set, the
  root element is the wrapper and `Class` is merged onto the avatar *inside* it.
  A consumer's margin or grid-placement class therefore applies to the wrong box
  in exactly the configuration where the component's root changes. The same is
  true of `AdditionalAttributes`.
- **The size scale is written in literals, in two languages.** `24px`, `28px` and
  `40px` with their font sizes live in the `.avatar` rules in `dryl.css`, and the
  matching icon sizes are bare integers in the component's `IconSize` switch.
  Nothing relates the two, so a size change means editing CSS and C# and hoping
  they still agree (`DESIGN-01`).
- **The ring assumes the ground is `--bg-0`.** Both the avatar's outer ring and
  the dot's ring paint `--bg-0` to punch a gap out of whatever is behind them.
  On a card, a toolbar or an app bar that is the wrong color, and the ring reads
  as a hairline of page background rather than as a gap.
- **Nothing is animated.** The component has no enter, exit, hover or state
  transition of any kind — not the status dot appearing, not the fallback taking
  over from a failed image (`DESIGN-11`, `DESIGN-12`). This is recorded as debt
  rather than claimed as the exception `DESIGN-11` allows: an avatar is not a
  component with nothing to animate, it is one that was never animated.
- **No tests of its own.** None of the criteria above is guarded by a test, and
  the avatar is absent from `tests/DRYL.Components.Tests/ClassMergeTests.cs`
  despite carrying a `Class` parameter.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--accent-grad`, `--on-accent`,
  `--success`, `--danger`, `--warning`, `--fg-faint` and `--glass-3` are the
  mode-dependent tokens; the component defines no mode-specific rule.
- **Enter/exit animation** — **absent**, and recorded above as debt rather than
  as an exception.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is `role="img"` with a derived label; the substantive
  omission is the silent presence dot, recorded above.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — `DRYL.Website/Components/Pages/DemoAvatar.razor`, with the
  examples `Components/Examples/Avatar/Faces.razor`, `.../Sizes.razor`,
  `.../Shape.razor`, `.../Presence.razor` and `.../Group.razor`.
- **`ComponentCatalog`** — registered as `"Avatar"` / `avatar` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged not AI-capable.
