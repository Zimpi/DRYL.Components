# I21 — Tool approval: implementation plan

Branch `feat/tool-approval` (DRYL.Components), `feat/tool-approval` (DRYL.Website).
Specs: `specs/E3 AI/F1 DrylToolCall.md`, `specs/E3 AI/F2 DrylToolCallGroup.md`,
`specs/E12 Agent Runtime/F1 DrylAgentToolCalls.md`, `specs/E12 Agent Runtime/_Api.md`.
Versions: core 3.7.0 → **3.8.0**, Agents 0.21.1 → **0.22.0**, one `[3.8.0]` changelog
block with `(Agents 0.22.0)` tags. Each task is one commit; T1 bumps both versions and
opens the block, later tasks join it (`REL-01`, the version has not shipped).

**T1 — `DrylToolCall` approval.** `code/DRYL.Components/Components/AI/DrylToolCall.razor`
(+ `.razor.css`): `OnApprove`, `OnDeny`, `ApproveLabel`, `DenyLabel`, `Declined`; action
row under the head inside `DrylPresence`; focus returns to the head row; `tool-call--declined`.
Tests: `tests/DRYL.Components.Tests/` bUnit for every criterion of the "Approval" section
and the new keyboard criteria. Spec F1 → `Implemented`. Verification:
`dotnet test DRYL.slnx -c Release --filter DrylToolCall`.

**T2 — `DrylToolCallGroup` approval.** Same pattern: `PendingCount`, `OnApproveAll`,
`OnDenyAll`, the two labels, the "awaiting approval" label and pill, auto-open on entering
`Proposed`. Tests per criterion. Spec F2 → `Implemented`. Verification: filtered test run.

**T3 — Agents runtime.** `Agents/DrylToolInvocation.cs` (`DrylToolApproval`, `Approval`,
`State`), `Agents/DrylRunBase.cs` (`PendingApprovals`, `Approve`, `Deny`, the decision
gate), `Agents/DrylAgentRunner.Run.cs` (`ToolApprovalRequestContent`, the resume loop,
dedupe by call id, cancellation → declined), new `Replay` overload. Tests via
`StartFromUpdates`/`Replay` with a scripted `resume`, plus one through a real
`ChatClientAgent` over a fake `IChatClient` with `ApprovalRequiredAIFunction`, so the
framework's own request/response round trip is proven. Verification: filtered test run.

**T4 — `DrylAgentToolCalls` wiring.** Group state, `PendingCount`, `ActiveOnly`, card and
bulk callbacks, labels. bUnit tests. Spec E12 F1 → `Implemented`. Verification: full
`dotnet test DRYL.slnx -c Release`.

**T5 — website demos.** `DRYL.Website`: `Examples/ToolCall/Approval.razor`,
`Examples/ToolCallGroup/Approval.razor`, `Examples/Agents/ToolApproval.razor` (simulated
agent with `ApprovalRequiredAIFunction`), wired into their pages.
`dotnet test DRYL.Website.slnx`. Verification: the browser, both modes, keyboard path,
the wave from the button.

**T6 — review and close.** Independent review agent over the branch diff. Full evidence
run (`CLAUDE.md` stage 5). Changelog complete, specs `Implemented`.

## Progress

- [x] T1 · [x] T2 · [x] T3 · [x] T4 · [ ] T5 · [ ] T6

## Decisions taken during implementation

- 2026-10-17, T3: `ToolApprovalRequestContent.RequiresConfirmation` is marked
  experimental (`MEAI001`); the rest of the approval API is stable. The runner does
  not read it, so a future removal cannot break consumers at run time. Consequence:
  an informational-only request (rare, some hosted tools) also shows as pending and
  is answered like any other. The spec criterion about it was dropped.
