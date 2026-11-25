# A Markdown editor with a preview switch

## Meta
- **State:** Draft

## Problem

DRYL can show Markdown (`DrylMarkdown`) and it can take multi-line text
(`DrylTextarea`), but a host that lets a person *write* Markdown builds the
pairing itself every time: a text area, a rendered view, a switch between the
two, and the state that keeps them on the same string.

Target role: the Blazor developer. *(Open — which concrete screen needs it, and
who writes there: see Open Points.)*

## Solution Idea

A new input component — working name `DrylMarkdownEditor` — that binds one
Markdown string and shows it in one of two views, chosen by a switch in the
component's header:

- **Editor** — the source, in the library's text-area surface.
- **Preview** — the same string rendered by `DrylMarkdown`, with its
  untrusted-content guarantees unchanged.

First technical reading by the Tech Lead, not yet decided:

- Inherits `InputBase<string>` like `DrylTextarea`, so it binds with
  `@bind-Value`, validates inside an `EditForm`, and renders its own label,
  helper text and validation message.
- The switch is the existing `DrylSegmentedControl` (two segments, gliding
  thumb). The current view is bindable through a new enum, so a host can open
  in the preview or drive the view itself.
- The change between the two views is animated with the existing vocabulary
  (`DESIGN-11`, `DESIGN-12`).

## Scope

- **In scope:** the component, the view switch, the two views, form binding,
  spec, tests, changelog, a demo page in `DRYL.Website`.
- **Out of scope (proposed, to be confirmed):** WYSIWYG editing; syntax
  highlighting inside the source; image upload; any new dependency.
- **Undecided:** a formatting toolbar and keyboard shortcuts; a side-by-side
  view; an `Ai` parameter.

## Impact

Preliminary — completed in phase 3 once the scope is fixed.

- **Harness:** the two-view switch as described needs no new token, duration,
  easing, `AiState` or dependency: Markdig is already the approved exception
  (`CODE-03`) and is only used through `DrylMarkdown`. A formatting toolbar
  would need JS interop for the text selection (first-party script in
  `dryl.js`, no dependency) and possibly icons that do not exist yet.
- **Specs:** a new feature spec under `specs/E8 Inputs/` (next free number:
  F24); `_Api.md` of E8 gains the component and its enum; the component count
  in `harness/requirements.md` and in the coverage check rises by one.
- **Public API (new component, MINOR):** to be drafted — at least `Value`,
  `Label`, `Placeholder`, `HelperText`, `Rows`, `Disabled`, `AriaLabel`, the
  bindable view, `Class`, `AdditionalAttributes`.
- **Code:** `Components/Inputs/DrylMarkdownEditor.razor` (+ `.razor.css`),
  composing `DrylSegmentedControl` and `DrylMarkdown`. Risks: the source must
  survive a round trip through the preview (the text area unmounts or hides);
  focus must land sensibly after a switch; the preview's height should not
  make the page jump.

## Decisions

- 2026-11-25 — Jan: DRYL gets a Markdown editor with a switch between editor
  and preview.

## Open Points

- Which concrete screen needs the editor, and who writes in it — a person
  alone, or a person together with an AI (drafting, rewriting)? That decides
  whether the component takes an `Ai` parameter in its first version.
- Is the first version exactly "source + preview + switch", or does it carry a
  formatting toolbar (bold, italic, heading, list, link, code) and shortcuts?
- Is the view always one of two, or is a side-by-side view on wide screens
  part of it?
