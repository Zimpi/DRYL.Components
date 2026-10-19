# DrylCommandPalette

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Navigation/DrylCommandPalette.razor
              code/DRYL.Components/Components/Navigation/DrylCommandPalette.razor.css
              code/DRYL.Components/Components/Navigation/CommandGroupOrder.cs
              code/DRYL.Components/Components/Navigation/CommandPaletteTexts.cs
              code/DRYL.Components/Components/Navigation/CommandFuzzyMatcher.cs
              code/DRYL.Components/Components/Navigation/CommandRegistry.cs
              code/DRYL.Components/Components/Navigation/ICommandRegistry.cs
              code/DRYL.Components/Components/Navigation/ICommandResolver.cs
              code/DRYL.Components/Components/Navigation/CommandResolution.cs
              code/DRYL.Components/Components/Navigation/CommandContext.cs
              code/DRYL.Components/Models/CommandItem.cs

This spec is split (`SPEC-02`) — the palette is one of the three components
`SPEC-02` names as candidates. The acceptance criteria live in the `S{n}` files
beside this one; this file carries the `Meta` block, the description and the
public API. The state above is the rolled-up state.

| File | Subject |
|---|---|
| `S1 Overlay, motion and loading.md` | Opening and closing, the portal, enter and exit, rows entering and leaving, skeleton, quieted rows, reduced motion |
| `S2 Items, groups and fallback.md` | Static items and the search provider, keys, `Meta`, group order, the fallback, `OnItemRun`, debounce |
| `S3 Keyboard and accessibility.md` | Hotkey, arrows, Tab between groups, Enter, Escape, focus return, roles and names |
| `S4 Commands, arguments and the AI seam.md` | `DrylCommand` rows, the argument view, destructive confirmation, the resolver suggestion, the AI panel, texts |

The design came from `ideas/I25 A command palette that searches the host.md`.

## User Story

As a Blazor developer, I want one keyboard-first field that finds anything in my
application — records from my own data, pages, commands — and hands the query
to an assistant when nothing fits, so that my users have a single entry point
without the palette knowing anything about my application.

## Description

`DrylCommandPalette` is a launcher overlay. It opens on Ctrl+K / ⌘K or through
`@bind-Open`, shows a search field above a list of result rows grouped under
headers, and runs the row the user picks.

It is fed two equal ways, freely combined:

- **Items** — `CommandItem`s from a static `Items` array or an async
  `SearchProvider`. This is the path for search results: the host owns the data,
  the ranking and the order; the palette shows what it gets.
- **Commands** — declarative `DrylCommand` children (see
  [`../F5 DrylCommand.md`](../F5%20DrylCommand.md)) that register in the shared
  `ICommandRegistry`, fuzzy-matched by the palette and rendered after the items.

The palette stays a host-agnostic shell. The host supplies results, their order
(`GroupOrder`), one always-present `Fallback` entry ("Ask the assistant: …") and
what "recent" means: the palette stores nothing and reports every item that ran
through `OnItemRun`, so the host can return its own recent items as the result
of the empty query.

An optional `ICommandResolver` turns a natural-language query into one
confirmable command suggestion, and an AI result panel can be driven through
`Ai` and `AiContent`.

The AI-mode decision (`AI-05`): **yes** — the palette takes `Ai` and `Aura`
for the result panel and the shell aura, and shows the shared thinking
vocabulary in the search row while a resolver works.

The demo lives on the `DRYL.Website` page `/components/command-palette`, built
from `Components/Examples/CommandPalette/`, and the component is registered in
the `ComponentCatalog`.

## Public API

### Parameters

| Parameter | Type | Default | Meaning |
|---|---|---|---|
| `Open` | `bool` | `false` | Whether the overlay is shown. Two-way with `OpenChanged`. |
| `OpenChanged` | `EventCallback<bool>` | — | Fires when the palette opens or closes itself. |
| `Items` | `CommandItem[]?` | `null` | Static items, filtered by label, description and category. |
| `SearchProvider` | `Func<string, Task<CommandItem[]>>?` | `null` | Async provider; wins over `Items`. |
| `Debounce` | `TimeSpan` | 250 ms | Rest before a non-empty query reaches `SearchProvider`. |
| `GroupOrder` | `CommandGroupOrder` | `Alphabetical` | Order of the groups. |
| `Fallback` | `Func<string, CommandItem?>?` | `null` | One entry kept present for every non-blank query. |
| `OnItemRun` | `EventCallback<CommandItem>` | — | Fires for every item and fallback that runs. |
| `Texts` | `CommandPaletteTexts?` | `CommandPaletteTexts.Default` | Every word of the palette's own chrome. |
| `Placeholder` | `string?` | `null` | Wins over `Texts.Placeholder` when set. |
| `EmptyText` | `string?` | `null` | Wins over `Texts.EmptyText` when set. |
| `AriaLabel` | `string?` | `null` | Wins over `Texts.AriaLabel` when set. |
| `MaxResults` | `int` | `8` | Cap on registry commands per query; items are not capped. |
| `HotKey` | `bool` | `true` | Enables the global Ctrl+K / ⌘K toggle. |
| `Resolver` | `ICommandResolver?` | `null` | The AI seam for command suggestions. |
| `Ai` | `AiState` | `AiState.None` | State of the AI result panel and shell aura. |
| `Aura` | `AiAura?` | `null` | Aura variant; inherits a surrounding `DrylAiScope`. |
| `AiContent` | `RenderFragment?` | `null` | Content of the AI result panel. |
| `ChildContent` | `RenderFragment?` | `null` | Hosts `DrylCommand` children. |
| `Class` | `string?` | `null` | Extra classes on the panel. |

### JS-invokable members

`OnEscape()`, `OnGlobalOpen()`, `OnGroupKey(bool backwards)` and
`OnRowLeft(string key)` are public because `dryl.js` calls them; they are not
meant to be called by a consumer.

### Types

`CommandItem`, `CommandItemType`, `CommandGroupOrder`, `CommandPaletteTexts`,
`ICommandRegistry`, `ICommandResolver`, `CommandResolution` and
`CommandContext` are listed in [`../_Api.md`](../_Api.md).
