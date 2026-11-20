# DrylAiCommandPalette

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components.Agents/CommandPalette/DrylAiCommandPalette.razor
              code/DRYL.Components.Agents/CommandPalette/DrylAiCommandResolver.cs

## User Story

As a Blazor developer with declarative `DrylCommand`s and an `AIAgent`, I want a
command palette that already knows how to ask the agent which command a typed
sentence means, so that "mark this invoice as paid" becomes a confirmable
suggestion without me writing a resolver.

## Description

`DrylAiCommandPalette` is a thin wrapper: it renders one `DrylCommandPalette`
and hands it the `ICommandResolver` registered in DI. Everything the user sees —
the overlay, the list, the argument view, the sparkle-marked suggestion row, the
thinking pill — is the core palette's behaviour, specified in
[`../E10 Navigation/F4 DrylCommandPalette/_Component.md`](../E10%20Navigation/F4%20DrylCommandPalette/_Component.md)
and its `S4` story. This spec covers what the wrapper adds: the wiring, and the
resolver that ships with it.

`DrylAiCommandResolver` is the agents package's implementation of the core's
`ICommandResolver`. For one query it turns every enabled `DrylCommand` into an
`AIFunction` named after the command's id and asks the agent to call exactly
one. The function only **records** the choice — the model never executes a
command. The palette then shows the choice as a suggestion, and running it stays
a user action.

```csharp
builder.Services.AddDrylAgents()
                .AddDrylCommandResolver(sp => sp.GetRequiredService<MyAgent>());
```

```razor
<DrylAiCommandPalette @bind-Open="_open">
    <DrylCommand Title="Set status" OnRun="SetStatus">
        <DrylCommandArgument Name="status" Type="CommandArgType.Choice"
                             Options="@(new[] { "Draft", "Sent", "Paid" })" Required="true" />
    </DrylCommand>
</DrylAiCommandPalette>
```

## Public API

Namespace: `DRYL.Components.Agents`. Injects `ICommandResolver` (required).

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Open` | `bool` | `false` | Visibility; supports `@bind-Open`. |
| `OpenChanged` | `EventCallback<bool>` | — | Raised when the palette opens or closes. |
| `Placeholder` | `string?` | `null` | Search input placeholder, forwarded to the core palette; `null` leaves the core palette's `CommandPaletteTexts.Placeholder`. |
| `EmptyText` | `string?` | `null` | Empty-state text, forwarded to the core palette; `null` leaves the core palette's `CommandPaletteTexts.EmptyText`. |
| `Texts` | `CommandPaletteTexts?` | `null` | Every word of the palette's chrome; the place to translate it. |
| `MaxResults` | `int` | `8` | Maximum registry commands shown for a query. |
| `HotKey` | `bool` | `true` | Enables the global ⌘K / Ctrl+K toggle. |
| `Items` | `CommandItem[]?` | `null` | Static item list, used when `SearchProvider` is not set. |
| `SearchProvider` | `Func<string, Task<CommandItem[]>>?` | `null` | Async search provider; wins over `Items`. |
| `Debounce` | `TimeSpan` | 250 ms | Rest before a non-empty query reaches `SearchProvider`. |
| `GroupOrder` | `CommandGroupOrder` | `CommandGroupOrder.Alphabetical` | Order of the result groups. |
| `Fallback` | `Func<string, CommandItem?>?` | `null` | One entry kept present for every non-blank query. |
| `OnItemRun` | `EventCallback<CommandItem>` | — | Raised whenever an item runs. |
| `Ai` | `AiState` | `AiState.None` | AI result panel state of the palette shell. |
| `Aura` | `AiAura?` | `null` | AI aura variant; `null` inherits the surrounding scope. |
| `AiContent` | `RenderFragment?` | `null` | Content of the AI result panel. |
| `AriaLabel` | `string?` | `null` | ARIA label of the palette dialog. |
| `Class` | `string?` | `null` | Extra classes, forwarded to the core palette. |
| `ChildContent` | `RenderFragment?` | `null` | Declarative `DrylCommand` children. |

`DrylAiCommandResolver`, `AddDrylCommandResolver` and `ParseArguments` are
listed with their exact signatures in [`_Api.md`](_Api.md).

## Acceptance Criteria

### Wrapping

- The component renders exactly one `DrylCommandPalette` and no element of its
  own.
- Every parameter of the wrapper reaches the core palette unchanged: `Open`,
  `OpenChanged`, `Placeholder`, `EmptyText`, `Texts`, `MaxResults`, `HotKey`,
  `Items`, `SearchProvider`, `Debounce`, `GroupOrder`, `Fallback`, `OnItemRun`,
  `Ai`, `Aura`, `AiContent`, `AriaLabel` and `Class`.
- The wrapper has every `[Parameter]` of `DrylCommandPalette` except
  `Resolver`, each with the same type; a parameter added to the core palette
  and not to the wrapper fails
  `DrylAiCommandPaletteTests.Carries_every_parameter_of_the_core_palette_except_the_resolver`.
- `Texts` set translates the core palette's chrome.
- With `Placeholder` or `EmptyText` unset, the core palette shows its own
  default word from `CommandPaletteTexts.Default`.
- `ChildContent` is rendered inside the core palette, so its `DrylCommand`s
  register there.
- The core palette's `Resolver` is the `ICommandResolver` resolved from DI.
- Rendering the component without a registered `ICommandResolver` throws.
- `MaxResults` defaults to `8`.
- `HotKey` defaults to `true`.

### Registration

- `AddDrylCommandResolver(agentFactory)` registers `ICommandResolver` scoped.
- The registered resolver is a `DrylAiCommandResolver` over the agent the
  factory returns for that scope.
- `AddDrylCommandResolver` returns the same `IServiceCollection`, so calls chain.

### Resolving a query

- Disabled commands are not offered to the agent.
- With no enabled command, `ResolveAsync` returns `null` without creating a
  session or running the agent.
- Each enabled command is offered as one `AIFunction` whose name is the
  command's `ResolvedId`.
- The function's description starts with the command's `Title`.
- A non-blank `Description` follows the title in the function's description.
- Each argument appears in the function's description with its name and
  `CommandArgType`.
- A required argument is marked required in the function's description.
- A `Choice` argument's options appear in the function's description.
- A non-blank argument description appears in the function's description.
- Without a session factory, every resolution runs in a fresh session from the
  agent.
- With a session factory, every resolution runs in the session the factory
  returns.
- The agent is instructed to call exactly one function, and none when no
  command fits.
- Calling a function records the command and its arguments and returns a
  receipt; the command's `OnRun` is not invoked.
- When the agent calls no function, `ResolveAsync` returns `null`.
- When the agent calls a function, `ResolveAsync` returns a `CommandResolution`
  carrying that command.
- When the agent calls several functions in one run, the last call wins.
- The returned `Confidence` is `1.0`.
- The `CancellationToken` passed to `ResolveAsync` reaches the session factory
  and the agent run.
- An exception from the agent propagates out of `ResolveAsync`; the core palette
  is what keeps the manual results in that case.

### Argument parsing

- `ParseArguments` returns an empty dictionary when the JSON is not an object.
- An argument absent from the JSON is absent from the result.
- A property the command does not declare is ignored.
- A `Number` argument given as a JSON number becomes a `double`.
- A `Number` argument given as a string is parsed with the invariant culture;
  an unparseable string becomes `null`.
- A `Boolean` argument given as `true` or `false` becomes a `bool`.
- A `Boolean` argument given as a string is parsed; an unparseable string
  becomes `null`.
- A `Text` or `Choice` argument given as a string becomes that string.
- A `Text` or `Choice` argument given as any other JSON value becomes its raw
  JSON text.

### Keyboard, accessibility and motion

- Keyboard operation, focus handling, ARIA roles and the live announcements are
  the core palette's; the wrapper adds no focusable element and no ARIA
  attribute.
- Enter and exit animation, the suggestion row and the thinking pill's fade are
  the core palette's; the wrapper adds no motion of its own.

### Appearance

- The component contributes no CSS of its own.
- The component branches on no color mode and holds no mode-assuming value
  (`DESIGN-02`).

## Recorded debt

- **No session factory through DI.** `AddDrylCommandResolver` always builds the
  resolver without a session factory; session reuse needs a hand-registered
  `DrylAiCommandResolver`.
- **Confidence is constant.** `Confidence` is always `1.0`; it does not reflect
  the model's certainty.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — no markup or style of its own; the core palette's modes
  apply; inspected on `/components/ai-command-palette` in dark and light, with a resolved suggestion shown (2026-11-20).
- **Enter/exit animation** — delegated: the core palette's overlay enter/exit
  (`S1` of its spec); the wrapper renders nothing that mounts on its own.
- **Keyboard and a11y** — delegated to the core palette, as stated above.
- **AI mode** — **yes, through the resolver**: the AI here is the resolution
  step, whose thinking aura and pill the core palette already draws while
  `Resolver` works, with no opt-in. The core palette's own `Ai`, `Aura` and
  `AiContent` are forwarded for a host that also shows an AI result panel.
- **Demo page** — `/components/ai-command-palette`
  (`DRYL.Website/Components/Pages/DemoAiCommandPalette.razor`), example
  `Agents/AiCommandPalette`. The site has no model: the page runs on a scripted
  stand-in `ICommandResolver` registered in DI and says so.
- **`ComponentCatalog`** — the `"AI Command Palette"` / `ai-command-palette`
  entry in `DRYL.Website/Components/ComponentCatalog.cs`.
- **Tests** — `tests/DRYL.Components.Tests/DrylAiCommandPaletteTests.cs`
  (children reach the inner palette) and
  `tests/DRYL.Components.Tests/DrylAiCommandResolverTests.cs` (`ParseArguments`
  coercion). The resolution path through an agent has no test.
