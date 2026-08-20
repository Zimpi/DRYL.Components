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

*The remaining shared types of this category are filled in during phase C.*
