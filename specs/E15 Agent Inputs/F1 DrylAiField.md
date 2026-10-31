# DrylAiField

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components.Agents/Field/DrylAiField.razor
              code/DRYL.Components.Agents/Field/DrylAiField.razor.css
              code/DRYL.Components.Agents/Field/AiFieldPrompt.cs
              code/DRYL.Components.Agents/Field/AiFieldSnapshot.cs
              code/DRYL.Components.Agents/wwwroot/js/dryl-aifield.js

## User Story

As a Blazor developer, I want to put an AI affordance around any text input I
already have — without rewiring its binding — so that a user can let an agent
write or rewrite the field, watch the text arrive, and then accept or reject the
suggestion.

## Description

`DrylAiField` wraps one existing text-like DRYL input (or any markup containing
one `textarea` or text-like `input`) and adds a ✨ trigger in its top-right
corner. Pressing it runs `Agent` through `DrylAgentRunner`: with an empty field
the agent writes the value, with a selection only the selection is transformed.
The reply streams live into the field through a small DOM bridge that dispatches
native `input` events, so the wrapped component's `@bind-Value` updates itself.

The wrapped input sits inside a `DrylAiScope`, so its aura follows the run:
`Thinking` while the agent starts, `Streaming` while text arrives, and
**`Proposed`** once the suggestion is complete and waits for the user. Two
chips — accept and reject — slide in below the field. Accepting moves the aura
to `Generated`: the input plays the accept wave and the bloom, and its aura then
retires itself. Rejecting restores the field to its value before the run and
takes the aura out through the graceful exit.

```razor
<DrylAiField Agent="agent" Instruction="Rewrite professionally">
    <DrylTextarea @bind-Value="mail.Body" />
</DrylAiField>
```

Without `Instruction` (or with `ShowPrompt`) the trigger opens a mini-prompt in
a `DrylPopover` instead, where the user types what the agent should do.

## Public API

Namespace: `DRYL.Components.Agents`. Implements `IAsyncDisposable`. Requires
`AddDrylAgents()` (for `DrylAgentRunner`).

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Agent` | `AIAgent` (`EditorRequired`) | — | The agent that generates or transforms the value. |
| `Instruction` | `string?` | `null` | Fixed instruction; `null` opens the mini-prompt instead. |
| `ShowPrompt` | `bool` | `false` | Always opens the mini-prompt; `Instruction` becomes its prefill. |
| `Context` | `string?` | `null` | Extra context sent with the prompt. |
| `SessionFactory` | `Func<CancellationToken, Task<AgentSession>>?` | `null` | Session reuse; `null` creates a fresh session per run. |
| `TriggerLabel` | `string` | `"Mit AI ausfüllen"` | Tooltip and accessible name of the trigger at rest. |
| `CancelLabel` | `string` | `"Abbrechen"` | Tooltip and accessible name of the trigger while a run can be cancelled. |
| `SendLabel` | `string` | `"Senden"` | Tooltip and accessible name of the mini-prompt's send button. |
| `PromptPlaceholder` | `string` | `"Was soll die AI tun?"` | Placeholder and accessible name of the mini-prompt input. |
| `AcceptLabel` | `string` | `"Übernehmen"` | Label of the accept chip. |
| `RejectLabel` | `string` | `"Verwerfen"` | Label of the reject chip. |
| `Disabled` | `bool` | `false` | Hides the affordance; the wrapped input renders unchanged. |
| `Class` | `string?` | `null` | Extra classes merged onto the wrapper's own. |
| `ChildContent` | `RenderFragment?` | `null` | The wrapped input. |
| `OnAccepted` | `EventCallback<string>` | — | The user accepted; receives the field's value at that moment. |
| `OnRejected` | `EventCallback` | — | The user rejected (chip, Escape, or cancel mid-run). |
| `OnError` | `EventCallback<DrylRunError>` | — | The run failed; the field has been restored. |

## Acceptance Criteria

### Structure

- The component renders a `.ai-field` wrapper with `ChildContent` inside a
  `DrylAiScope`.
- `Class` is merged onto the wrapper's classes.
- While a run is in flight or a suggestion is under review, the wrapper carries
  `.ai-field--on`.
- The trigger is an icon-only ghost `DrylButton` with the `Sparkle` glyph,
  wrapped in a `DrylTooltip`; its tooltip and `aria-label` are `TriggerLabel`.
- `Disabled` renders no trigger and leaves the wrapped input unchanged.
- Initially neither the review chips nor the mini-prompt are rendered.
- The trigger and the mini-prompt are marked `data-aifield-ui`, so the bridge
  never mistakes them for the wrapped field.

### Running

- With `Instruction` and without `ShowPrompt`, pressing the trigger starts a
  run with `Instruction`.
- Without `Instruction`, or with `ShowPrompt`, pressing the trigger opens the
  mini-prompt; with `ShowPrompt` it is prefilled with `Instruction`.
- In the mini-prompt, Enter or the send button starts a run with the typed text;
  a blank text starts nothing.
- The field is the first enabled `textarea` or text-like `input` (no type,
  `text`, `search`, `email`, `url`, `tel`) inside the wrapper, outside
  `data-aifield-ui`.
- Without such a field, pressing the trigger does nothing.
- A run starts at most once: a second press while one is starting is ignored.
- The prompt carries the instruction, `Context`, the field's current value and,
  with a selection, the selected portion, and asks for the replacement text only.
- With a selection, only the selected portion is replaced; the text before and
  after it is kept.
- A reply wrapped in one pair of double quotes or one fenced code block is
  unwrapped.
- During the run the field is read-only and the trigger shows the `X` glyph
  with `CancelLabel`.
- Text is written into the field as it arrives, at most every 50 ms, and in full
  once the reply is complete.

### AI state of the wrapped input

- When the run starts, the wrapped input shows `AiState.Thinking`.
- With the first arriving text it shows `AiState.Streaming`.
- Once the suggestion is complete it shows **`AiState.Proposed`**: the resting
  aura, with I20's description for assistive technology.
- Accepting moves it to `AiState.Generated`: the accept wave plays, then the
  `Generated` bloom, and the aura retires itself (`AI-07`).
- Rejecting, cancelling and a failed run move it to `AiState.None` through the
  graceful exit.

### Review

- A complete suggestion shows the accept and reject chips below the field,
  sliding in.
- The trigger does nothing during review; the chips are the affordance.
- Accepting fires `OnAccepted` with the field's value at that moment, including
  edits the user made during review, and focuses the field.
- Rejecting restores the value the field had before the run, focuses the field
  and fires `OnRejected`.
- Escape during review rejects.
- The chips slide out when review ends.

### Cancelling and errors

- Pressing the trigger or Escape during a run cancels it, restores the field
  and fires `OnRejected`.
- A failed run restores the field, shows the error's message below it and fires
  `OnError`.
- The error message is cleared when the next run starts.

### Accessibility

- A visually hidden polite live region announces the run starting, the
  suggestion being ready, its acceptance, rejection and failure.
- The trigger, the chips and the send button are native buttons, reachable by
  Tab.
- The trigger keeps its accessible name in sync with its glyph.

### Appearance and motion

- The trigger fades and scales in on hover or focus within the wrapper over
  `--dur-fast`, and stays visible while `.ai-field--on`.
- Under `prefers-reduced-motion: reduce` the trigger appears without a
  transition.
- Every color, space and duration comes from a token; the component branches on
  no color mode (`DESIGN-02`).

### Cleanup

- Disposal cancels a run in flight.
- The JS module is disposed only if it was loaded, so static prerender does not
  throw.

## Recorded limits

- **The default strings are German** (`TriggerLabel`, `CancelLabel`,
  `SendLabel`, `PromptPlaceholder`, `AcceptLabel`, `RejectLabel` and the live
  announcements), unlike every other default in the library. Changing them would
  change what every existing consumer shows; recorded in
  `ideas/I31 Proposed wherever the library waits for a decision.md` and left for
  its own decision. The announcements have no parameter at all.
- The field is located by selector, so a wrapped component that renders several
  text inputs is driven through its first.
- The accepted value is read back from the field; a wrapped component that
  rewrites its value on input (a mask) reports the rewritten value.

## Cross-cutting evidence (`SPEC-05`)

- **Both modes:** colors come from `DrylButton`'s variants and the shared aura;
  no mode branch. Not checked by eye in this pass.
- **Motion:** trigger fade/scale, chips via `DrylPresence` (`SlideUp`), the
  wrapped input's aura through `DrylAiScope`; `Proposed` rests without motion.
- **AI:** the wrapped input follows `Thinking` → `Streaming` → `Proposed` →
  `Generated` / `None` (`AI-01`, `AI-06`, `AI-07`).
- **Tests:** `tests/DRYL.Components.Tests/Agents/DrylAiFieldTests.cs`,
  `AiFieldPromptTests.cs`, `AiFieldSnapshotTests.cs`.
