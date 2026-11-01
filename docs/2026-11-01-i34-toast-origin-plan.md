# I34 — Toasts with an origin: implementation plan

Idea: [`ideas/I34 A toast redesign.md`](../ideas/I34%20A%20toast%20redesign.md)
(variant C, confirmed by the Product Owner on 2026-11-01).
Branch: `feat/i34-toast-origin`, merged into `release/3.16.0`.
Version: 3.16.0 (block already open on the release branch, `REL-01`).

## Behaviour being built

- A toast with an origin (`ToastOptions.Origin`) grows out of that element's
  shape and moves to its place in the stack.
- A toast without an origin comes out of the notification bell — a visible
  `DrylNotifications` bell in service-driven mode, marked `data-dryl-inbox`.
- Without such a bell, it unfolds from a capsule around its icon in place.
- A toast that came out of the bell, expires unread and carries text, folds
  into a capsule, flies back into the bell and becomes an unread entry in
  `IDrylNotificationService`. `ToastOptions.KeepInInbox = false` opts out.
- Every other leave (close button, `Close()`, `CloseAll()`, expiry of a toast
  with an origin) keeps today's shrink-blur-fade.
- Neighbours glide to their new place on every add and remove (FLIP).
- Each toast keeps its own countdown: removing one never restarts another's
  bar (`@key` moves to the loop's top-level `CascadingValue`).
- `prefers-reduced-motion`: fades only; the inbox entry is still made.
- No new token, no new CSS keyframe: the choreography runs as Web Animations in
  `dryl.toast`, on `--dur-*`, `--ease-out`, `--ease-spring` and `--ease-viscous`.

## Tasks

| # | Task | Files | Verification | Commit |
|---|---|---|---|---|
| 1 | Idea to `Ready` | `ideas/I34 A toast redesign.md` | `node scripts/check-harness-links.mjs` | `ideas: I34 Ready` |
| 2 | Specs: first spec for each | `specs/E11 Surfaces/F2 DrylToast.md`, `specs/E1 Foundation/F3 DrylToastProvider.md`, `specs/E11 Surfaces/_Api.md`, `_Interop.md`, `specs/E1 Foundation/_Api.md`, `specs/E7 Feedback/F8 DrylNotifications.md` | `node scripts/check-spec-coverage.mjs` (meter rises 72 → 74) | with task 3 (`SPEC-01`) |
| 3 | Code: options, provider, bell marker, JS choreography, CSS, `@key` fix | `Toasts/ToastOptions.cs`, `Components/Providers/DrylToastProvider.razor`, `Components/Surfaces/DrylToast.razor`, `Components/Feedback/DrylNotifications.razor`, `wwwroot/js/dryl.js`, `wwwroot/dryl.css` | `dotnet build DRYL.slnx -c Release` | one commit with specs, tests, changelog |
| 4 | Tests | `tests/DRYL.Components.Tests/DrylToastProviderTests.cs`, `tests/js/toast.test.mjs` | `dotnet test DRYL.slnx -c Release`, `node --test tests/js/*.test.mjs` | in task 3's commit |
| 5 | Changelog in the open 3.16.0 block | `CHANGELOG.md` | review | in task 3's commit |
| 6 | Browser check, both modes | scratch Blazor host referencing the library (outside the repo) | Playwright: entry from button, from bell, capsule fallback, expiry into bell + inbox entry, bars do not restart | — |
| 7 | Remaining checks, merge into `release/3.16.0`, push | — | light-sync, contrast, motion tokens, harness links | merge commit |
