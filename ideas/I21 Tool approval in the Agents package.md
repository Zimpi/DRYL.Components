# Tool approval in the Agents package

## Meta
- **State:** Adopted
- **Specs:** `specs/E3 AI/F1 DrylToolCall.md`, `specs/E3 AI/F2 DrylToolCallGroup.md`,
  `specs/E12 Agent Runtime/F1 DrylAgentToolCalls.md`, `specs/E12 Agent Runtime/_Api.md`

## Problem

I20 gave DRYL a resting AI state for "the AI is done, a human must accept or
dismiss" (`AiState.Proposed`, core 3.7.0) and deferred the Agents package to its
own step. Looking at that package for this idea turned up a sharper problem than
"show the new state somewhere":

**The framework's own tool approval is silently broken in DRYL.**
Microsoft.Extensions.AI (10.10, pulled in by Microsoft.Agents.AI 1.25.0) lets a
consumer wrap any tool in `ApprovalRequiredAIFunction`. The agent then does not
run the tool; it emits a `ToolApprovalRequestContent` and waits for a
`ToolApprovalResponseContent` on the same session. `DrylAgentRunner` has no
`case` for either: the request is dropped, the run reaches `AiState.Generated`
as if it had finished, and the tool call row never appears. A consumer who does
the right thing — "deleting a customer needs a click" — gets a run that just
stops, with nothing on screen.

The only human-in-the-loop path today is `DrylUiTools.RequestPermission`: a
tool the *model* chooses to call, which opens a modal `DrylConfirmDialog`.

**For whom:** developers building agent UIs on `DRYL.Components.Agents` with
tools that change things (write, delete, send, pay); and their end users, who
need to see *which* call waits for them and *what* it would do, where it
happens — in the run, not in a modal over it.

**Without the feature:** approval-required tools cannot be used with DRYL's
runner at all; consumers fall back to the modal tool or build their own loop.

## Solution Idea

1. **Runner.** `DrylAgentRunner.Start` recognises `ToolApprovalRequestContent`.
   The wrapped call becomes a `DrylToolInvocation` in a new lifecycle stage
   *awaiting approval* → `State` = `AiState.Proposed`. While any call is
   pending, the run itself rests as `Proposed` (so `DrylAiIndicator` reads
   "Proposed") instead of falsely reaching `Generated`.
2. **Decision API on the run.** `Approve(callId)`, `Deny(callId)` and
   `PendingApprovals`. Once the **last** pending call has a decision, the
   runner sends all `ToolApprovalResponseContent`s on the same `AgentSession`
   and keeps streaming into the **same** `DrylAgentRun` — one run, one
   transcript. An approved call then runs (`Thinking` → `Generated`); a denied
   call reaches the model as the framework's failed function result.
3. **Row (core `DrylToolCall`).** New optional approve/deny callbacks; the
   buttons render only in `Proposed`, next to the call's arguments. Approve →
   `Generated` with the I20 "Welle" from the click.
4. **Declined.** A denied call ends quietly: `AiState.None`, the status label
   "Declined", no danger alert; the aura leaves through the existing graceful
   exit.
5. **Group (core `DrylToolCallGroup`).** Optional "approve all" / "deny all"
   callbacks, shown in the summary from two pending calls on. A bulk approval's
   wave starts at the centre of each row. While calls are pending the summary
   shows `Proposed` ("2 awaiting approval") and the group **opens by itself**;
   if the user closes it, it stays closed.
6. **Wrapper (`DrylAgentToolCalls`).** Wires the run's API to row and group,
   and counts pending calls as visible — also under `ActiveOnly`.

The I20 anti-decoration guard holds by construction: `Proposed` appears only on
a row or summary that carries the accept and dismiss buttons.

## Scope
- **In scope:**
  - `ToolApprovalRequestContent` / `ToolApprovalResponseContent` in
    `DrylAgentRunner.Start` (single-agent run), resumption on the same session;
  - `DrylAgentRun.Approve` / `Deny` / `PendingApprovals`;
  - *awaiting approval* and *declined* stages on `DrylToolInvocation`;
  - approve/deny on `DrylToolCall`, approve all / deny all on
    `DrylToolCallGroup`, auto-open of the group;
  - `DrylAgentToolCalls` wiring, `ActiveOnly` including pending calls;
  - cancelling a run while calls are pending ends them as declined;
  - button labels as parameters with English defaults (cf. board card #67);
  - a website demo on `DRYL.Website`.
- **Out of scope:**
  - canvas operations — the canvas has no accept/dismiss model today; that gets
    its own idea;
  - `StartSequential` / `StartConcurrent` — their specs state the limitation;
  - `DrylUiTools.RequestPermission` — stays as it is;
  - calendar stage 3 (I19);
  - hosted MCP approval modes (`HostedMcpServerTool*ApprovalMode`);
  - approvals that survive a reload or a new circuit (pending state lives in
    the run object).

## Impact
- **Harness:** no new token, animation, `AiState` or dependency. `Proposed`,
  the still aurora edge, the wave and the graceful exit exist since 3.7.0; the
  buttons are `DrylButton`s in existing variants; the auto-open reuses the
  group's existing expand motion. `AI-04`: the "Declined" end state is
  `AiState.None` plus a label — no new AI visual.
- **Specs:** `specs/E3 AI/F1 DrylToolCall.md` and
  `specs/E3 AI/F2 DrylToolCallGroup.md` (new stories, State → Modified);
  `specs/E12 Agent Runtime/` — runner, run and `DrylToolInvocation` have no
  component spec yet (phase C); the spec stage writes the ones this idea
  touches, plus `DrylAgentToolCalls`. `_Api.md` of E3 and E12 updated.
- **Public API:** additive only. Core: approve/deny callbacks and label
  parameters on `DrylToolCall`, approve all/deny all callbacks and labels on
  `DrylToolCallGroup` → core **3.8.0**. Agents: `Approve`, `Deny`,
  `PendingApprovals` on the run, the two new lifecycle stages on
  `DrylToolInvocation` → Agents **0.22.0**, minimum core 3.8.0.
- **Code:** `Agents/DrylAgentRunner.Run.cs` (new `case`, resume loop),
  `Agents/DrylToolInvocation.cs`, the run class, `Agents/DrylAgentToolCalls.razor`;
  core `Components/AI/DrylToolCall.razor`, `DrylToolCallGroup`.
  Risks: resuming the stream while the old enumerator ends; cancellation while
  pending; the framework rule that every call in one response needs approval
  once one does (so many pending rows); the content type was renamed from
  `FunctionApprovalRequestContent` between Microsoft.Extensions.AI 10.2 and
  10.6, so a future rename is possible. Testable without a model via the
  runner's internal `UpdatesFactory` seam.

## Decisions
- 2026-10-17 (Product Owner): **final version confirmed** ("Ja passt") — the three Tech Lead defaults stand; the idea is `Ready`.
- 2026-10-17 (Product Owner): the Agents adoption of `Proposed` is the next step
  after I20 ("Mach deine Empfehlung").
- 2026-10-17 (Product Owner): **scope is framework-native tool approval only.**
  Canvas operations get their own idea — the canvas has no accept/dismiss model
  yet, and inventing one is a product decision of its own.
  *Alternative rejected:* tool approval and canvas together — markedly larger.
- 2026-10-17 (Product Owner): **Approve/Deny live in the core `DrylToolCall`**
  as new optional callbacks, rendered only in `Proposed` — the buttons sit on
  the surface the wave runs across, and core users without Agents get them too.
  *Alternative rejected:* Agents-only buttons around the core row — the wave
  and the action would sit on different surfaces.
- 2026-10-17 (Product Owner): **`DrylUiTools.RequestPermission` stays as it
  is.** The modal carries that decision; its row keeps `Thinking`, so `Proposed`
  stays where the buttons are (I20 anti-decoration guard).
  *Alternative rejected:* the row shows `Proposed` while the dialog is open —
  more honest about who is working, but the decision happens in the modal.
- 2026-10-17 (Product Owner): **each row decides on its own, plus an "approve
  all".** The run resumes once the last pending call has a decision. The wave
  of a bulk approval starts at the centre of each row (the I20 fallback when the
  click lies outside the surface).
  *Alternative rejected:* rows only — with every call of a response needing
  approval, approving harmless reads one by one is tedious.
- 2026-10-17 (Product Owner): **a denied call ends quietly** — a new "declined"
  lifecycle stage on `DrylToolInvocation`, rendered as `AiState.None` with the
  label "Declined" and no danger alert; the aura leaves through the existing
  graceful exit. Denying is a decision, not a failure.
  *Alternative rejected:* `Error = "Declined by user"` — no new stage, but it
  reads as a failure.
- 2026-10-17 (Product Owner): **API on the run: `Approve(callId)`,
  `Deny(callId)`, `PendingApprovals`.** `DrylAgentToolCalls` wires them to
  `DrylToolCall` itself.
  *Alternative rejected:* one `Respond(callId, bool)` — a bare `true`/`false`
  reads poorly at the call site.
- 2026-10-17 (Product Owner): **the group opens by itself** while calls are
  pending; a user who closes it keeps it closed. `ActiveOnly` shows pending
  calls too.
  *Alternative rejected:* the group stays closed and the summary carries
  everything — it invites approving without having seen the arguments.
- 2026-10-17 (Product Owner): **"Approve all" and "Deny all" in the core
  `DrylToolCallGroup` summary**, as optional callbacks, from two pending calls
  on. Denying all is the safe way out.
  *Alternatives rejected:* approve all only; a bulk button rendered by the
  Agents wrapper outside the group — breaks the decision to keep the actions in
  the core.

- 2026-10-17 (Tech Lead, while writing the specs): an approved call moves
  **straight from `Proposed` to `Generated`** and stays there while the tool
  runs. The shared accept wave plays only on that direct transition
  (`AuraLifecycle.FromProposed`); routing through `Thinking` would lose it and
  replay the comet. The tool runs at the start of the resumed request, before
  the model is called again, so the window is short.
- 2026-10-17 (Tech Lead, while writing the specs): the approval buttons render
  in a row of their own under the head, because the head row is a `<button>`
  and cannot hold buttons.

## Tech Lead notes from the dialogue
- 2026-10-17: `FunctionInvokingChatClient` documents that once one call in a
  model response needs approval, **every** call in that same response needs
  approval too, harmless ones included. Several pending rows at once are the
  normal case. A denied call reaches the model as a failed function result
  generated by the framework — the run goes on.
- 2026-10-17: `DrylAgentToolCalls` wraps every call in a **collapsed**
  `DrylToolCallGroup`; the summary follows only `Thinking`/`Streaming` calls,
  and `ActiveOnly` filters to `Thinking`. Without a change, pending rows would
  sit hidden inside a closed group, and vanish entirely under `ActiveOnly`.
- 2026-10-17, closed by the Tech Lead as defaults (open for the PO to
  overrule at confirmation): cancelling a run while calls are pending ends them
  as *declined*; button labels are parameters with English defaults;
  orchestrated runs are out of scope and documented as such.

## Open Points
