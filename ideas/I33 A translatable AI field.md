# A translatable AI field

## Meta
- **State:** Adopted
- **Specs:** `specs/E15 Agent Inputs/F1 DrylAiField.md`

## Problem

`DrylAiField` (Agents package, `Field/DrylAiField.razor`) is the only component in
the library whose defaults are German, and part of its wording cannot be changed
at all:

- **Six string parameters with German defaults:** `TriggerLabel` ("Mit AI
  ausfüllen"), `CancelLabel` ("Abbrechen"), `SendLabel` ("Senden"),
  `PromptPlaceholder` ("Was soll die AI tun?"), `AcceptLabel` ("Übernehmen"),
  `RejectLabel` ("Verwerfen").
- **Five live announcements, hard-coded, no parameter:** "Die AI schreibt …",
  "AI-Vorschlag bereit — übernehmen oder verwerfen.", "Vorschlag übernommen.",
  "Vorschlag verworfen." (reject and cancel), "Die AI-Anfrage ist fehlgeschlagen."
  A screen reader hears German in every application, whatever its language.
- The XML doc's example instruction is German too ("Formuliere professioneller").

The spec records it as a limit (`specs/E15 Agent Inputs/F1 DrylAiField.md`,
*Recorded limits*), I31 deferred it. The website's own demo
(`Components/Examples/Agents/AiField.razor`) uses the defaults, so the English
documentation site shows German buttons.

## Solution Idea

A record `AiFieldTexts` after `CommandPaletteTexts` (and `CanvasTexts`): every
human word the field renders of its own in one place, English defaults, one
`Texts` parameter.

```csharp
public sealed record AiFieldTexts
{
    public static AiFieldTexts Default { get; } = new();

    public string Trigger           { get; init; } = "Fill with AI";
    public string Cancel            { get; init; } = "Cancel";
    public string Send              { get; init; } = "Send";
    public string PromptPlaceholder { get; init; } = "What should the AI do?";
    public string Accept            { get; init; } = "Accept";
    public string Reject            { get; init; } = "Discard";

    // live region
    public string Writing           { get; init; } = "The AI is writing…";
    public string Ready             { get; init; } = "AI suggestion ready — accept or discard.";
    public string Accepted          { get; init; } = "Suggestion accepted.";
    public string Rejected          { get; init; } = "Suggestion discarded.";
    public string Failed            { get; init; } = "The AI request failed.";
}
```

`[Parameter] public AiFieldTexts? Texts`, resolved as `Texts ?? AiFieldTexts.Default`.

**The six existing string parameters** — three options:

- **A — Keep them, they win when set (recommended).** Exactly the palette's
  precedent: `Placeholder`/`EmptyText`/`AriaLabel` stay and win over the matching
  `Texts` member. Their default becomes `null` (type `string?`), so "unset" falls
  through to `Texts`. Nothing a consumer wrote stops compiling.
- **B — Mark them `[Obsolete]`, pointing at `Texts`.** Same as A now, removed
  later. Two ways to say the same thing are noise; the obsolete warning guides
  people to one. Costs a deprecation cycle.
- **C — Remove them.** Cleanest API, but a compile break for every consumer who
  set one.

**Migration note (changelog, *Changed*):** "The default wording of `DrylAiField`
is now English, and its screen-reader announcements can be translated. To keep
the German texts, pass
`Texts="AiFieldTexts.Default with { Trigger = "Mit AI ausfüllen", … }"`" — with
the full German set spelled out in the entry so it can be copied, since the
previous wording otherwise exists nowhere once the defaults change.

## Scope

- **In scope:** `AiFieldTexts` record, `Texts` parameter, English defaults, the
  five announcements, the spec's limit replaced by ACs, the website demo (shows
  English by default; optionally a second example passing German `Texts`), tests
  for defaults / override / precedence.
- **Out of scope:**
  - The agent's instruction and output — those are the host's content.
  - The error text from `DrylRunError.Message` — produced by the runner, not by
    the field.
  - A library-wide localisation mechanism (`IStringLocalizer` & co.) — the
    `*Texts` record pattern is the established one.

## Impact

- **Harness:** no token, motion, `AiState` or dependency. Removes the one
  violation of the English-default convention.
- **Specs:** `specs/E15 Agent Inputs/F1 DrylAiField.md` — parameter table gains
  `Texts`, the six string parameters change type/default, the *Recorded limits*
  bullet becomes ACs (each announcement comes from `Texts`; a set legacy
  parameter wins over its member).
- **Public API:** new type `AiFieldTexts`, new parameter `Texts`. Under A the six
  parameters change from `string` (non-null default) to `string?` (default
  `null`) — source-compatible for anyone assigning a string; a consumer *reading*
  the property gets `null` instead of German text (rare, but a change).
  `DRYL.Components.Agents` is **0.x** (0.24.0), so a visible behaviour change is a
  MINOR (`REL-01`), not gated by the core's 1.0 API freeze.
- **Behaviour:** every consumer relying on the defaults sees English buttons and
  hears English announcements after the update. That is the point of the change,
  and the reason it needs the migration note.
- **Code:** `Field/DrylAiField.razor` (+ a new `Field/AiFieldTexts.cs`); no CSS,
  no JS. The palette's resolution (`Words => Texts ?? Default`) is copied.

## Decisions

The Product Owner delegated every open point to the Tech Lead on 2026-10-31
("Beantworte die ganzen Fragen bitte selbst … mach das alles fertig"); the
decisions below were taken under that delegation.

- 2026-10-31: **Option A** — the six label parameters stay, become `string?`
  with a `null` default, and win over `Texts` when set. The palette precedent,
  and no consumer's code breaks. No `[Obsolete]`: a set parameter is a
  legitimate one-off override, not a deprecated path.
- 2026-10-31: **"Write with AI"** for the trigger — it covers filling an empty
  field and rewriting a selection, which "Fill" does not. **"Accept" /
  "Discard"** for the chips: "Discard" says the suggestion goes away without
  sounding like a judgement of it.
- 2026-10-31: **No language presets.** The library ships none for the palette or
  the canvas; the changelog carries the complete German set to copy.
- 2026-10-31: **MINOR for the Agents package (0.25.0)**, entry under *Changed*
  with the migration snippet — 0.x, and the change is the purpose, not a side
  effect.

## Open Points

*(none)*
