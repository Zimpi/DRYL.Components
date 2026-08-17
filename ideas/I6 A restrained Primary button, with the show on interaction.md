# A restrained Primary button, with the show on interaction

## Meta
- **State:** Adopted
- **Carried into:** specs/E2 Actions/F1 DrylButton.md
                    specs/E2 Actions/_Api.md
                    specs/E2 Actions/F3 DrylSplitButton.md

## Problem

`DrylButton`'s two loudest variants sit too far apart, and the loud one is loud
at the wrong moment.

**The gap.** `ButtonVariant.Primary` is a full `--accent-grad` fill with a
four-layer accent shadow; `ButtonVariant.Secondary` is neutral glass on
`--line-strong` and carries no accent at all until it is hovered. An action that
matters but is not *the* one action on the surface has nowhere to sit. A
consumer either overstates it as a Primary or understates it as a Secondary.

**The louder half of the problem.** The Product Owner does not like how Primary
looks today, and the reason is legible in the stylesheet: the entire show already
runs at rest. `.btn-primary`'s resting `box-shadow` carries `0 6px 24px` at 35 %
`--accent-a` plus an `0 0 48px` halo at 18 % `--accent-b`, before the user has
done anything. Hovering can then only offer *more of the same* — 50 % and 64px.
The button spends its whole budget standing still, and has nothing left for the
moment a user actually engages with it. What is wanted is the inverse: quiet at
rest, and something worth looking at on hover and on press.

These are one problem, not two. A new variant "between Primary and Secondary"
that is quiet-then-expressive **is** the better Primary; adding it beside today's
Primary would leave two variants competing for the same role and make the
hierarchy harder to read than it is now, not easier.

## Solution Idea

Redefine `ButtonVariant.Primary` as a **gradient hairline on glass**, and preserve
today's filled treatment under a new name.

- **Primary (redefined).** The surface is `--glass-2` with `--glass-fx-flow`,
  the same ground `ButtonVariant.Secondary` stands on. What distinguishes it is a
  1px **`--accent-grad` border** — the accent as a hairline, which is exactly the
  form `DESIGN-08` names — and a label in `--fg`. At rest it carries **no glow at
  all**.
- **The show, on interaction — "Aufladen".** On hover the accent gradient washes
  *into* the surface as a tint, so the button visibly moves toward `Bold` without
  ever arriving there; the accent glow ignites from zero at the same time, and the
  existing `--shimmer` sheen sweeps the glass. On press the button drops and
  shrinks with its existing spring.

  The tint is written as a `linear-gradient` whose two stops sit at `0 %` accent at
  rest and at `26 %` / `22 %` on hover, so the transition interpolates two images
  of the same shape rather than swapping one in. Its ceiling is a deliberate limit,
  not a taste: a tint is `DESIGN-08`-compliant, a fill is not, and the fill belongs
  to `Bold`.
- **Bold (preserved).** Today's `--accent-grad` fill with its four-layer shadow
  becomes `ButtonVariant.Bold`, for the rare hero call to action that genuinely
  should shout. Nothing about its appearance changes; only its name and its role
  do.

The gradient hairline is drawn as a `::before` ring masked with
`mask-composite: exclude`, over a `border-color: transparent`. `.btn::before` is
unused today (`.btn::after` belongs to the sheen), and the masked-ring technique
already appears four times in `dryl.css`, so this introduces no new method. The
naive alternative, `border-image`, is rejected: it does not follow
`border-radius` and would square off the button's corners.

The travelling gradient is animated as a rotating element behind the mask, never
as an animated custom property or `background-position`, per the compositor rules.

## Scope

- **In scope:**
  - Redefining `.btn-primary`'s appearance and interaction in
    `code/DRYL.Components/wwwroot/dryl.css`.
  - Adding `ButtonVariant.Bold` carrying today's filled treatment unchanged.
  - Updating the 15 in-library `ButtonVariant.Primary` call sites where the new
    quiet Primary is not the right weight — assessed per call site, not
    rewritten wholesale.
  - Updating `specs/E2 Actions/F1 DrylButton.md`, whose "Appearance" and "Motion"
    criteria describe the old Primary in detail and would otherwise contradict the
    code (`SPEC-01`).
  - `specs/E2 Actions/F3 DrylSplitButton.md` inherits the enum and gains `Bold`
    for free; its spec is checked for statements about Primary's look.

- **Out of scope:**
  - A press ripple originating at the pointer position. It needs a CSS variable
    fed by JS interop per button, and `DrylButton` deliberately has no interop
    today (it is a pure `.razor` with no codebehind). Not worth that cost for
    this idea; revisit separately if the press still feels thin.
  - `Secondary`, `Ghost` and `Danger`, which are not part of the complaint.
  - The `.btn--active` toggle treatment, other than confirming it stays
    distinguishable from the new Primary — it is a flat `--accent-line` ring plus
    glow, where the new Primary is a *gradient* ring with no resting glow, so the
    two do not collide.
  - New tokens, new durations, new easings. None are needed.

## Impact

- **Harness:** **No blocker.** No new token — `--accent-grad`, `--accent-a`,
  `--accent-b`, `--glass-2`, `--glass-fx-flow` and `--shimmer` all exist and are
  maintained in both LIGHT-TOKEN-SET copies. No new duration or easing: hover and
  press stay on `--dur-med` / `--dur-fast` / `--dur-slow` with `--ease-out` and
  `--ease-spring`. No new `AiState`. No new dependency. Nothing loops, so nothing
  needs `DESIGN-10`'s exemption for continuous animation. The mask literals fall
  under `DESIGN-01`'s alpha-context exemption, which names `mask` explicitly.

  One thing to watch rather than a blocker: the new Primary's label moves from
  `--on-accent` on a saturated fill to
  `--fg` on `--glass-2`, so `node scripts/validate-light-contrast.mjs` is the
  gate that must stay green in light mode, where `--glass-2` is
  `rgba(255,255,255,0.62)` rather than a dark wash.

- **Specs:** `specs/E2 Actions/F1 DrylButton.md` (`State: Implemented` →
  `Modified` on change) — its "Appearance" section pins Primary to
  `--accent-grad` fill, `--on-accent` label, `--on-accent-line` border and the
  resting accent glow, and its "Motion" section pins the hover lift; all of those
  criteria are rewritten, and criteria for `Bold` are added. Its enum criteria
  ("accepts exactly the four values", "each of the four variants") become five.
  `specs/E2 Actions/F3 DrylSplitButton.md` is checked for the same.
  No new category and no new component.

- **Public API:** Additive — `DrylButton.ButtonVariant` gains `Bold`. No rename
  and no removal, so this is MINOR under `REL-01`, not MAJOR. `DrylSplitButton`
  types its own `Variant` as `DrylButton.ButtonVariant` and therefore exposes
  `Bold` without a change of its own. The *visual* meaning of the existing
  `Primary` value changes, which no version number expresses — it belongs in
  `CHANGELOG.md` as a called-out behavioural change so a consumer upgrading is not
  surprised by their buttons looking different.

- **Code:** `code/DRYL.Components/wwwroot/dryl.css` is the substantive change;
  `DrylButton.razor` gains one enum member and one variant class mapping and is
  otherwise untouched. Fifteen `ButtonVariant.Primary` call sites across eleven
  files are reviewed: `CanvasNodeView.razor`, `DrylButton.razor`,
  `DrylCommandPalette.razor`, `DrylChatComposer.razor`, `DrylAlertDialog.razor`,
  `DrylConfirmDialog.razor`, `DrylCanvasDock.razor`, `DrylAiField.razor`,
  `DrylAskChoiceDialog.razor`, `DrylAskMultiChoiceDialog.razor`,
  `DrylAskTextDialog.razor`. All are inside the library; no consumer code in this
  repo breaks.

  Risks: (1) the confirm/alert dialogs rely on Primary to mark the affirmative
  action against a Ghost cancel — the new Primary must stay clearly the stronger
  of the two, which is the criterion to verify by eye rather than assume;
  (2) `--glass-fx-flow` resolves to `none` by default in dark mode, so the new
  Primary's ground is a flat 5 % white wash unless blur is enabled — the design
  must read well without the blur, not only with it;
  (3) `DRYL.Website` lives outside this repository and carries the demo page and
  screenshots, so a follow-up pass there is required and cannot be verified from
  here.

## Decisions

- 2026-08-17: The idea is **not** a fifth variant between Primary and Secondary,
  but a redefinition of Primary itself. Reason: the Product Owner's dissatisfaction
  with Primary's look and the missing middle weight are the same problem; solving
  them separately would leave two variants competing for one role.
- 2026-08-17: Primary's new look is the **gradient hairline on glass**, chosen
  over a tonal `--accent-soft` fill and over a plain `--accent-line` outline.
  Reason: the tonal fill would read like `.btn--active`, which is already an
  accent border plus glow; the outline sits nearer to Ghost than to Primary and
  closes the gap only halfway.
- 2026-08-17: Today's filled treatment is **preserved as `ButtonVariant.Bold`**
  rather than deleted. Reason: a hero call to action still needs a loud option,
  and deleting it would remove a capability rather than refine one.
- 2026-08-17: The chosen interaction is the **glow igniting from zero on hover**
  and the **existing sheen sweep**. The pointer-positioned press ripple is rejected,
  as the only option on the table that would force JS interop into a component that
  has none.
- 2026-08-17: A first comparison page offered six variations of the hairline. The
  Product Owner rejected the set as **too similar to one another** — six lightings
  of one idea rather than six ideas — and kept two: "Kernglühen" (an inner radial
  bloom) and "Aufladen" (the surface tinting toward `Bold`).
- 2026-08-17: A second page offered six genuinely divergent directions — the accent
  cast *beneath* the button, a directional wipe-in fill, a 3px accent rail, a pill
  silhouette with letter-spacing motion, an inset-to-raised physical inversion, and
  a body-less gradient-text button.
- 2026-08-17: **"Aufladen" is the decision.** Reason given by the Product Owner: it
  is the most restrained of the field and at the same time the most attractive. It
  therefore also supersedes the travelling-gradient interaction chosen earlier: the
  hairline stays static, and the movement lives in the surface tint. This removes
  the one cost that direction carried — the extra DOM node `DrylButton` would have
  had to emit for a rotation behind the mask. The component's markup is unchanged.
- 2026-08-17: The divergent directions are **not** carried forward as further
  variants. They were decision material for this one choice; reviving any of them
  later starts as its own idea.
- 2026-08-17: An earlier naming decision in this dialogue settled on `Accent` for
  a new middle variant. It is **superseded**: that variant no longer exists, and
  the name that had to be chosen instead is `Bold`, for the loud one.

- 2026-08-17: The Product Owner **confirmed the redefinition of `Primary`**
  explicitly, having been told that every existing Primary — in the library, in
  the dialogs, and in consumer applications — changes appearance on upgrade.
- 2026-08-17: `Bold` is **fully visible**: a first-class fifth variant in the demo,
  the documentation and the catalog, not an advanced option hidden in the spec.
  Reason (Product Owner): a consumer who wants the loud button should be able to
  find and use it. This overrides the Tech Lead's recommendation to keep it
  discreet, which argued that a prominent `Bold` invites consumers back to the
  loud variant and reopens the problem later. Recorded as a deliberate trade, not
  an oversight.
- 2026-08-17: Before the spec is written, the Product Owner picks the exact
  resting and hover treatment from a side-by-side comparison page of concrete
  variations. The idea fixes the *direction* — quiet at rest, gradient hairline,
  show on interaction — and the comparison fixes the execution.

## Open Points

- None. The exact variation is chosen from the comparison page during Stage 2
  and recorded in the spec; the idea itself has no unresolved question.
