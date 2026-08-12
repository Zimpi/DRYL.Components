# Inputs API Audit (vs CONVENTIONS.md) — 2026-06-10

Category: **Inputs (23 components)**. Audited against `CONVENTIONS.md` for the
1.0 API freeze (board #39). Discovery only — no component code changed.

Status legend: ✅ conforms · ⚠️ deviation (needs fix) · 🔵 intentional / minor.

## Summary

Inputs is in good shape. Every two-way-binding event already follows
`<Property>Changed` (no `On`/`Is` prefix): `ValueChanged`, `TagsChanged`,
`SelectedValuesChanged`, `FilesChanged`, `RangeStartChanged`, `RangeEndChanged`.
Booleans are plain adjectives (`Disabled`, `Multiple`, `Required`, `Range`,
`ShowStepper`, `ShowValue`, `Block`, `AllowClear`). The `AiState Ai` parameter is
present on every value-bearing input and correctly absent from the non-AI
structural controls (`DrylCheckbox`, `DrylRadio`, `DrylToggle`, `DrylSegment`,
`DrylSegmentedControl`, `DrylFormField`, `DrylValidationSummary`) per CLAUDE.md
§2.10.

**Only 2 real deviations + 1 minor.** No `@bind`-blocking issues found.

## Deviations to fix (feeds the Inputs fix plan)

| Component | Item | Rule (§) | Status | Proposed change |
| --- | --- | --- | --- | --- |
| `CONVENTIONS.md` (self) | §2 example wrote `Readonly` | §2 | ⚠️ doc | The library uses `ReadOnly` (`DrylRating.ReadOnly`), the standard .NET casing. Fix the convention doc to `ReadOnly`. (Fixed in this commit — see below.) |
| `DrylSegmentedControl` | `string? Class` pass-through | §2 | ⚠️ | Replace the ad-hoc `Class` parameter with the standard `[Parameter(CaptureUnmatchedValues = true)] IDictionary<string,object>? AdditionalAttributes` (or, if a typed `Class` is desired, add it library-wide — but the convention is `AdditionalAttributes`). Only component in Inputs using `Class`. |

## Minor / intentional

| Component | Item | Rule (§) | Status | Note |
| --- | --- | --- | --- | --- |
| `DrylSegmentedControl` | `SegmentedSize` enum | §1 | 🔵 | Convention is `<Component><Concept>` → `SegmentedControlSize`. Shared conceptually with `DrylSegment`; rename is low-value churn. Decide at fix time; lean keep. |
| Several | explicit `string? AriaLabel` param | §2 | 🔵 | Present on most inputs, absent on pure containers (`DrylFormField`, `DrylValidationSummary`). Consistent enough; `aria-label` also flows via `AdditionalAttributes`. No change. |
| `DrylSegment` vs `DrylSegmentedControl` | `Value` nullability (`TValue?` vs `TValue`) | §2 | 🔵 | Item vs control; both correct for their role. No change. |

## Conformance highlights (✅, no action)

- Binding events: all `*Changed`, no `On`/`Is` prefix.
- `Value`/`ValueChanged` two-way binding via `InputBase<TValue>` across the
  text/number/select family.
- `AiState Ai` naming + presence/absence correct per §2.10 and §4.
- `RenderFragment? ChildContent` default slot; `RenderFragment<TItem>? ItemTemplate`
  context slot on `DrylAutocomplete` (§5).
- `IDictionary<string,object>? AdditionalAttributes` used for pass-through
  (e.g. `DrylChipInput`).

## Outcome

Inputs needs essentially **one component fix** (`DrylSegmentedControl.Class`) plus
the self-correcting doc casing fix. The Inputs fix plan will be short.
