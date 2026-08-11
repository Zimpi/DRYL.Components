# DrylCommandPalette — Design Spec

**Date:** 2026-06-29
**Status:** Approved (design), proceeding to implementation plan
**Scope:** A new, dependency-free command palette (⌘K) in `DRYL.Components`, plus an opt-in AI/natural-language layer in `DRYL.Components.Agents` that exposes registered commands to an agent as tool calls.

---

## 1. Goal in one paragraph

Give DRYL consumers a first-class **command surface**: a glassy, keyboard-driven ⌘K overlay where the user searches and runs application actions. The actions are declared once as `DrylCommand`s — and that single declaration serves three callers (mouse click, keyboard `Enter` on a fuzzy match, and the AI) with one execution path. The library's "AI-native" thesis is realised here for the first time as an actual *interaction surface* rather than a decorative state: an optional layer in the Agents package turns every registered command into an `AIFunction`, so a natural-language query ("markiere Acme als bezahlt") is resolved by the model into a **real tool call** with filled arguments. Per the consumer's decision, v1 surfaces that tool call as a confirmable top suggestion (human-in-the-loop), never auto-firing it. The core palette has **zero** AI dependency; the AI is injected through a narrow interface, honouring rule 2.8.

---

## 2. The central idea: `DrylCommand` *is* the tool

There are not two parallel worlds (palette entries vs. agent tools). There is **one unit of functionality**. A `DrylCommand` carries everything both roles need:

| Field on `DrylCommand` | Palette role | Agent/tool role |
| ---------------------- | ------------ | --------------- |
| `Title`                | Primary label | Tool display/selection hint |
| `Description`          | Secondary line | Tool description sent to the model |
| `DrylCommandArgument` children | Argument inputs (manual fill) | JSON schema of the `AIFunction` |
| `Keywords`             | Extra fuzzy-search aliases | — |
| `Shortcut`             | Display hint ("⌘N") | — |
| `Group`                | Section grouping | — |
| `Destructive`          | De-prioritised in sort; styled | Forces HITL confirm |
| `Disabled`             | Greyed, unselectable | Excluded from tool list |
| `OnRun(CommandContext)`| The one execution | The one execution |

Three ways in, one way out:

```
 Click  ─┐
 Enter  ─┼─►  DrylCommand.OnRun(CommandContext ctx)   // ctx.GetArgument<T>("status")
 Agent  ─┘
```

The consumer **builds functionality once** (as `DrylCommand` + optional `DrylCommandArgument`) and the agent can call **only** those registered commands — never anything unregistered.

---

## 3. Package split

| Layer | Package | Contents |
| ----- | ------- | -------- |
| Palette engine | `DRYL.Components` (zero-dep) | `DrylCommandPalette`, `DrylCommand`, `DrylCommandArgument`, `CommandContext`, `ICommandRegistry`/`CommandRegistry`, `ICommandResolver`, `CommandResolution`, fuzzy matcher, ⌘K hotkey JS |
| AI layer | `DRYL.Components.Agents` (opt-in) | `DrylAiCommandResolver : ICommandResolver`, `DrylAiCommandPalette` convenience wrapper |

The seam is **`ICommandResolver`**, defined in core. If `DrylCommandPalette.Resolver` is `null` → pure fuzzy matching. If an `ICommandResolver` is supplied → the AI resolves the query. The core never references `Microsoft.Agents.AI`.

---

## 4. Core public API (`DRYL.Components`)

### 4.1 `DrylCommand` (declarative child component + model)

```razor
<DrylCommand Title="Neue Rechnung" Icon="plus" Group="Erstellen"
             Shortcut="⌘N" OnRun="@CreateInvoice" />

<DrylCommand Title="Status setzen" Description="Status einer Rechnung ändern"
             Destructive="true" OnRun="@SetStatus">
    <DrylCommandArgument Name="target"  Description="Welche Rechnung(en)" Required />
    <DrylCommandArgument Name="status"  Description="Neuer Status"
                         Options="@(["Draft","Sent","Paid"])" Required />
</DrylCommand>
```

Parameters: `Title` (required), `Description?`, `Icon?` (DrylIcon name), `Group?`, `Keywords?` (`string[]` aliases), `Shortcut?`, `Destructive` (bool, default false), `Disabled` (bool), `OnRun` (`EventCallback<CommandContext>`), `Id?` (stable id; auto-generated if absent), `ChildContent` (hosts `DrylCommandArgument`s).
Behaviour: on first render registers itself into the ambient `ICommandRegistry` (cascaded from the palette); on `Dispose` removes itself. Implements the prerender dispose guard pattern (`_attached` flag) — no JS in this component, but symmetrical lifecycle.

### 4.2 `DrylCommandArgument`

Declarative child of `DrylCommand`. Parameters: `Name` (required), `Description?`, `Type` (`CommandArgType` enum: `Text` / `Number` / `Boolean` / `Choice`, default `Text`), `Required` (bool), `Options?` (`string[]`, for `Choice`). Produces (a) a manual input rendered in the palette when the command needs args, and (b) one property in the tool's JSON schema.

### 4.3 `CommandContext`

Passed to `OnRun`. Exposes the resolved arguments and a cancellation token:

```csharp
public sealed class CommandContext
{
    public IReadOnlyDictionary<string, object?> Arguments { get; }
    public T? GetArgument<T>(string name);
    public CancellationToken CancellationToken { get; }
}
```

Manual runs supply args from the rendered inputs (empty dict if the command has none); AI runs supply the model-filled args.

### 4.4 `ICommandRegistry` (scoped service)

```csharp
public interface ICommandRegistry
{
    IReadOnlyList<DrylCommand> Commands { get; }
    void Add(DrylCommand command);
    void Remove(string id);
    event Action? OnChanged;
}
```

Registered in `AddDrylComponents()`. Both the declarative `DrylCommand`s and any consumer code (for dynamic, context-dependent commands — e.g. "aktuelle Zeile löschen" from a deep child) feed the same registry. The palette renders the union, de-duplicated by `Id`.

### 4.5 `DrylCommandPalette`

Parameters:

| Parameter | Type | Default | Purpose |
| --------- | ---- | ------- | ------- |
| `Open` / `OpenChanged` | `bool` | `false` | Two-way visibility |
| `HotKey` | `bool` | `true` | Enable the global ⌘K / Ctrl+K toggle |
| `Placeholder` | `string` | "Befehl suchen…" | Input placeholder |
| `EmptyText` | `string` | "Keine Treffer" | Empty state |
| `MaxResults` | `int` | 8 | Results shown |
| `Resolver` | `ICommandResolver?` | `null` | AI seam — when set, NL resolution is active |
| `ChildContent` | `RenderFragment?` | — | Hosts declarative `DrylCommand`s |
| `Class` | `string?` | — | Merged class param (rule: class-splat clobber) |

The palette cascades the `ICommandRegistry`, hosts the `DrylCommand` children (which self-register), runs the fuzzy matcher over `registry.Commands`, and (if `Resolver != null`) calls the resolver for NL queries.

### 4.6 `ICommandResolver` + `CommandResolution` (the AI seam)

```csharp
public interface ICommandResolver
{
    Task<CommandResolution?> ResolveAsync(
        string query, IReadOnlyList<DrylCommand> commands, CancellationToken ct);
}

public sealed record CommandResolution(
    DrylCommand Command,
    IReadOnlyDictionary<string, object?> Arguments,
    double Confidence);
```

Core ships **no** implementation. The palette treats a non-null resolution as the highlighted top suggestion (with `.ai-aura` while awaiting, `.ai-indicator` pill, `aria-live="polite"`). Designed so a future agent-loop resolver (multiple tool calls) is a drop-in replacement without a core change.

---

## 5. AI layer (`DRYL.Components.Agents`)

### 5.1 `DrylAiCommandResolver : ICommandResolver`

Implements `ResolveAsync` by:

1. Converting each `DrylCommand` into an `AIFunction` whose name = command id, description = `Title` + `Description`, parameters = schema built from its `DrylCommandArgument`s. Reuses the schema-embedding approach already proven in `DrylAgentRunner.CreateUpdateTool<T>`.
2. Running a **single** structured resolution: the model selects one command and fills its arguments (v1 = one tool call, per the agent-depth decision). Returns the chosen `DrylCommand` + filled args + confidence. The tool is **not** invoked here — execution is deferred to user confirmation in the palette.
3. Reduced-motion-aware AI status surfaced through the core's resolution path.

### 5.2 Confirmation / HITL

- Non-destructive: the suggestion runs on `Enter`/click.
- `Destructive == true`: a `DrylConfirmDialog` (existing HITL building block, the same one `DrylUiTools.RequestPermission` uses) gates execution.

### 5.3 `DrylAiCommandPalette`

Thin wrapper that injects a DI-registered `DrylAiCommandResolver` and forwards everything to `DrylCommandPalette` (so consumers can either set `Resolver=` themselves or just use this). Registered via the existing `AddDrylAgents()`.

---

## 6. Visual & motion (rules 2.3, 2.4, 2.12)

- **Overlay portaled to `<body>`** (`position: fixed`), mirroring `DrylPopover`'s portal pattern (always-mounted wrapper + two-key visibility gate) to escape card `overflow`/`backdrop-filter` clipping. Glass surface (`--glass-*`, `--glass-blur`), 1px `--line` border, backdrop dim.
- **Enter/exit** via `DrylPresence` (`Scale` + fade) — animates *out*, not just in.
- **Results list** appears with a subtle stagger; the **active-row highlight glides** between rows via `dryl.motion.moveIndicator` (the moving marker is `aria-hidden`), never jumps.
- **AI state**: while the resolver awaits, the input wears `.ai-aura` and a `.ai-indicator` "Denkt…"-pill (`AiState.Thinking`) → `Streaming` if/when partial; reuses the shared AI vocabulary, invents nothing (rule 2.10).
- Only the fixed `--dur-*` / `--ease-*` vocabulary; `prefers-reduced-motion: reduce` fully honoured (primitives already do; mirror in any scoped CSS).
- Accent appears only as the input focus ring, the active-row 1px line/glow, and the AI aura — never a filled surface (rule 2.4).

---

## 7. Accessibility (rules 2.9, 2.11)

- Full keyboard: `↑`/`↓` navigate results, `Enter` run, `Esc` close (through `DrylPresence` exit), `⌘K`/`Ctrl+K` toggle.
- Focus trap inside the overlay while open; focus returns to the prior element on close.
- Roles: input `role="combobox"` + `aria-expanded`/`aria-controls`; list `role="listbox"`, rows `role="option"` with `aria-selected`.
- `aria-live="polite"` announces AI resolving/result changes.
- Any icon-only affordance inside (e.g. a close button) gets a `DrylTooltip` + matching `aria-label` (rule 2.11).
- Moving indicator is decorative (`aria-hidden`); animation never alters focus order or semantics.

---

## 8. JS interop

`window.dryl.commandPalette` in `wwwroot/js/dryl.js`: `register(dotNetRef, hotkeyEnabled)` attaches a global `keydown` listener for ⌘K/Ctrl+K that invokes a .NET callback to toggle `Open`; `unregister()` detaches. Dispose guarded by the `_attached` flag so static prerender does not throw (established prerender JS dispose pattern). No new external dependency.

---

## 9. Out of scope (YAGNI for v1)

- **Recent / frequently-used** ranking and persistence.
- **Multi-step agent loop** (multiple chained tool calls) — the `ICommandResolver` seam is deliberately shaped to accept it later without a core break.
- Nested/sub-commands (drill-down menus).
- Per-command async availability predicates beyond `Disabled`.

---

## 10. Files (anticipated)

**`DRYL.Components`**
- `CommandPalette/DrylCommandPalette.razor` (+ `.razor.css`, `.razor.cs`)
- `CommandPalette/DrylCommand.razor` (+ `.razor.cs`)
- `CommandPalette/DrylCommandArgument.cs`
- `CommandPalette/CommandContext.cs`
- `CommandPalette/CommandArgType.cs`
- `CommandPalette/ICommandRegistry.cs` + `CommandRegistry.cs`
- `CommandPalette/ICommandResolver.cs` + `CommandResolution.cs`
- `CommandPalette/CommandFuzzyMatcher.cs`
- `wwwroot/js/dryl.js` — add `commandPalette` module
- `ServiceCollectionExtensions` — register `CommandRegistry` in `AddDrylComponents()`

**`DRYL.Components.Agents`**
- `CommandPalette/DrylAiCommandResolver.cs`
- `CommandPalette/DrylAiCommandPalette.razor`
- `AddDrylAgents()` — register `DrylAiCommandResolver`

**Docs / demo**
- `CHANGELOG.md` — `[Unreleased] / Added`
- `ComponentCatalog` (DRYL.Website) — register `DrylCommandPalette`
- `samples/Pages/DemoCommandPalette.razor` — static commands, dynamic-via-service, AI mode

---

## 11. Definition of done

- [ ] Core palette works with `Resolver = null` (fuzzy only), zero AI dependency, builds dependency-free.
- [ ] Declarative `DrylCommand` + `ICommandRegistry` both feed one de-duplicated list.
- [ ] `DrylAiCommandResolver` turns commands into `AIFunction`s and resolves one tool call with filled args; execution deferred to confirm; `Destructive` → `DrylConfirmDialog`.
- [ ] Enter/exit, gliding active row, AI aura — all on the fixed motion vocabulary; reduced-motion clean.
- [ ] Keyboard + ARIA complete; ⌘K hotkey prerender-safe (dispose guard).
- [ ] `CHANGELOG.md`, `ComponentCatalog`, sample page updated.
- [ ] Tests green; Release build clean.
