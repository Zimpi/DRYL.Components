# Structured Streaming Showcase — "Living Recipe Card" (Design)

**Date:** 2026-07-08
**Status:** Approved by maintainer
**Scope:** DRYL.Website demo (`Components/Examples/Agents/StructuredGeneration.razor`) + one small
addition to `DRYL.Components.Agents` (raw buffer on `GenerationSnapshot<T>`).

## Goal

Turn the "Structured streaming with `DrylAiGenerate<T>`" section on `/components/agents` into the
flagship proof that DRYL's structured streaming streams **Blazor components, not text**: the model
emits JSON tokens, and a full recipe UI made of real DRYL components materialises live from the
growing typed snapshot. A collapsible raw-JSON panel shows the tokens arriving in sync — the
visible proof of *JSON in → components out*.

## Non-goals

- No new framework feature for field-level "just arrived" glow — the demo detects newly-arrived
  fields locally by comparing snapshots.
- No changes to `PartialJsonReader` / `JsonPartialRepair` — they already handle nested objects and
  object arrays.
- No new CSS tokens or motion primitives — the demo composes existing ones
  (`DrylPresence`, `DrylReveal`, `.ai-aura*`, `--dur-*`, `--ease-*`).

## 1. Framework addition (DRYL.Components.Agents)

`GenerationSnapshot<T>` gains one read-only property:

```csharp
/// <summary>The raw accumulated model output (the JSON buffer streamed so far).</summary>
public string Raw { get; internal set; } = "";
```

`DrylAiGenerate<T>.Apply(...)` passes `reader.Buffer` through on every chunk (empty string while
Thinking / after reset). Covered by a unit test in `DrylAiGenerateTests`. This is a MINOR feature:
bump `DRYL.Components.Agents` version, changelog entry under `Added`.

## 2. Data model (website, `SimScenarios.cs`)

`Recipe` grows to exercise every JSON shape the reader supports:

```csharp
Recipe {
  string? Title; string? Description;
  int Minutes; int Serves; int Difficulty;   // 1–5
  int Calories;
  Nutrition? Nutrition;                      // { int ProteinG; int CarbsG; int FatG; }
  List<string>? Tags;
  List<Ingredient>? Ingredients;             // { string? Amount; string? Name; }
  List<RecipeStep>? Steps;                   // { string? Title; string? Text; }
}
```

`SimScenarios.RecipeJson()` streams a matching recipe for the dish photo (one-pan lemon garlic
salmon with asparagus and cherry tomatoes). **Field order is choreography:** title → description →
minutes/serves/difficulty/calories → tags → ingredients → steps → nutrition (finale: the donut
animates last). Chunked ~6 chars every ~25 ms (≈5–6 s total).

## 3. The demo (`StructuredGeneration.razor`)

One glass recipe card that assembles itself while streaming:

- **Hero image** `dish.jpg` (copied to `DRYL.Website/wwwroot/img/`): blur/scale reveal when the
  stream starts; wears the shared AI aura until `Generated`.
- **Title + description** grow character by character (free from `PartialJsonReader`).
- **Meta row**: `DrylBadge` (minutes, serves, kcal) + `DrylRating` (difficulty) — each pops in via
  `DrylPresence` when its value first arrives.
- **Tags**: `DrylBadge` chips, entering one by one.
- **Ingredients**: list entries animating in individually.
- **Steps**: `DrylTimeline`, items fill in as they stream.
- **Nutrition**: `DrylDonutChart` (protein / carbs / fat) animates as the finale.
- After completion: `Generated` reveal → settle; button becomes "Generate again" (assigning a new
  stream reference restarts `DrylAiGenerate`).
- **Raw-JSON x-ray**: collapsible `DrylExpansion` showing `snap.Raw` live in monospace with a
  streaming caret, auto-scrolled to the end.

All motion uses the fixed vocabulary and honours `prefers-reduced-motion`.

## 4. Docs

- Sharpen the `DemoExample` description on `DemoAgents.razor`: structured streaming streams
  *components*, not just text.
- Changelog (Agents package): `Added` — `GenerationSnapshot<T>.Raw`.

## Error handling

- Stream failure: existing `DrylAiGenerate` behaviour (holds last good snapshot, settles to
  `None`) — demo needs no extra handling; the sim stream never faults.
- Partial numbers (e.g. `25` momentarily parsing as `2`) are inherent to partial JSON and
  acceptable in a live-updating UI; the choreography keeps such windows to a few ms.

## Testing

- Unit test: `GenerationSnapshot.Raw` grows with the buffer and is complete at `Generated`.
- Existing `DrylAiGenerateTests` stay green.
- Runtime verification via the docs website (watch the full stream, re-run, reduced-motion check).
