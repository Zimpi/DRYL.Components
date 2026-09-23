# Agent Canvas — Public API

Shared enums, parameter contracts and services of the Agent Canvas category — the
part of the data contract the 1.0 freeze binds.

**Source folder:** `code/DRYL.Components.Agents/Canvas/`

*Scaffold. The shared types below are filled in during phase C, each listed with
the exact spelling used in code. Until then this file claims nothing: it carries
no `Meta` block and the coverage check does not treat it as covering a
component (`SPEC-03`).*

## Shared types

### `DockCorner`

Which corner of the viewport `DrylCanvasDock` floats in. Source:
`code/DRYL.Components.Agents/Canvas/DockCorner.cs`.

| Member | Meaning |
|---|---|
| `BottomRight` | Bottom right — the default resting place for a command bar. |
| `BottomLeft` | Bottom left. |
| `TopRight` | Top right. |
| `TopLeft` | Top left. |

`BottomRight` is first and is therefore the enum's default value; the member
order is bound by the 1.0 freeze.

### `DrylCanvasRun` — the surface signal

The run is the handle the canvas tools write into and `DrylAiCanvas` reads.
Source: `code/DRYL.Components.Agents/Canvas/DrylCanvasRun.cs`. The members below
are the contract between a host that shows the canvas on demand and the canvas
that measures itself.

| Member | Meaning |
|---|---|
| `AvailableWidth` | `int?` — the measured width of the mounted canvas body in CSS px; `null` before the first measurement and after the measuring canvas is gone. Every generation reads it for its layout budget. |
| `ReportWidth(int)` | Records a width. A width of zero or less is ignored. Raises `OnWidthReported` and completes a pending `WaitForSurfaceAsync`. |
| `OnWidthReported` | `event Action<int>` — raised with every accepted width. |
| `WaitForSurfaceAsync(TimeSpan, CancellationToken)` | `Task<bool>` — `true` at once when a width is known, `true` when one is reported before the timeout, `false` when the timeout passes first; cancellation throws `OperationCanceledException`. |

### `DrylCanvasTools.BeforeGenerate`

`Func<CancellationToken, Task>?`, default `null`. Awaited at the start of every
`create_artifact`, `update_artifact` and `open_view` call, before the generation
begins and before it reads `AvailableWidth`; it receives the tool call's
cancellation token. An `update_artifact` refused for lack of an artifact does not
call it. Source: `code/DRYL.Components.Agents/Canvas/DrylCanvasTools.cs`.

*The remaining shared types of this category are filled in during phase C.*
