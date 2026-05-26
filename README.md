# DRYL

An open-source UI component library for **Blazor Server** and **Blazor WebAssembly** with an unapologetically modern, dark aesthetic.

> **Status: Early development — not production-ready.**
> DRYL is being built in the open. The design system is in place and several reference components exist, but the library is **not yet suitable for production use**. Expect breaking changes, missing components, and rough edges until `1.0`.

---

## Vision

DRYL is **dark, glassy, alive — and AI-native**.

Most Blazor component libraries feel like ports of Bootstrap or Material — safe, neutral, indistinguishable. DRYL is the opposite. Surfaces are translucent layers stacked on pure black, accents glow in a violet-to-cyan gradient, and motion is intentional rather than decorative.

The goal is a small, opinionated set of components — buttons, cards, inputs, tables, modals, navigation — that look like they belong in a product built in 2026, not 2014. And because 2026 products are increasingly driven by language models and tool calling, every DRYL surface knows how to **wear its AI state**: a card filled by a model breathes with a rotating gradient border, an input bound to a streaming completion glows while tokens arrive, and a generated block reveals itself with a one-shot accent wash. The result is a system where you can feel which parts of the UI are alive with AI without ever reading a label.

Every component reads from a single token file ([`dryl.css`](DRYL.Components/wwwroot/dryl.css)), so the entire visual language can be re-tuned in one place.

**Principles**

- **Token-driven.** Every color, spacing, radius, shadow and duration is a CSS variable. No magic numbers.
- **Dark only.** No light theme. Dark is the design, not a toggle.
- **Glass surfaces.** Translucent layers with `backdrop-filter`, never solid blocks.
- **AI-aware.** Every interactive surface can opt into an `AiState` — Active, Thinking, Streaming, Generated. The system signals AI presence consistently across components, so users learn the visual language once.
- **No JS frameworks.** Zero npm packages on top of Blazor — just CSS, Razor, and minimal interop.
- **Accessible by default.** Keyboard-reachable, ARIA-labeled, visible focus rings. AI activity is announced via `aria-live`.

---

## Preview

### Design system overview
![DRYL — design system overview](docs/screenshots/overview.png)

### Buttons
![DrylButton — variants and states](docs/screenshots/buttons.png)

### Cards
![DrylCard — glass surface with cursor spotlight](docs/screenshots/cards.png)

### Badges
![DrylBadge — neutral, accent, success, warning and danger variants](docs/screenshots/badges.png)

### Text input
![DrylInputText — form-bound text input with icon slots](docs/screenshots/inputtext.png)

### Form controls
![DrylCheckbox, DrylSelect, DrylTextarea and DrylToggle](docs/screenshots/form_controls.png)

### Tables
![DrylTable — generic table with sticky header, row selection and optional KPI summary bar](docs/screenshots/tables.png)

### AI Mode
![DRYL — AI Mode demo with lifecycle simulation and streaming rows](docs/screenshots/aimode.png)

### Dialog
![DrylDialog — service-driven glass dialog with Human-in-the-Middle AI flow](docs/screenshots/dialog.png)

---

## AI Mode — first-class citizen

DRYL treats AI as a **first-class state of the UI**, not an afterthought. Every surface in the library that can carry AI-generated content accepts a single `Ai` parameter of type `AiState`. That one parameter drives a consistent, learnable visual vocabulary — users see the same rotating gradient border on a card that's streaming tokens as they do on an expansion panel being filled by a tool call.

### The five states

| State        | Visual                                                          | When to use                                                |
| ------------ | --------------------------------------------------------------- | ---------------------------------------------------------- |
| `None`       | Default styling — no AI signal.                                 | Surface is rendered normally, unrelated to AI output.      |
| `Active`     | Slow rotating gradient border + breathing accent glow.          | Persistent AI-driven surface (a chat panel, an LLM card).  |
| `Thinking`   | Faster pulse on border and glow.                                | A tool call is in flight.                                  |
| `Streaming`  | Moderate pulse; content updates incrementally.                  | Tokens are arriving from the model.                        |
| `Generated`  | One-shot accent wash sweep + soft lift.                         | Reveal moment immediately after generation completes.       |

### AI-aware components

All AI-aware components share the same `Ai="AiState.X"` API. The effects are implemented entirely in CSS (`dryl.css`) with no component-specific overrides, so the ring, glow and wash look identical across:

- **`DrylCard`** — glass surface with cursor spotlight; ring draws around the card border
- **`DrylButton`** — rotating ring sits outside the variant fill; useful for "Ask AI" CTA buttons
- **`DrylInputText` / `DrylTextarea`** — ring wraps the input field; ideal for prompts bound to streaming completions
- **`DrylTable`** — ring wraps the full table; rows animate in as they stream
- **`DrylExpansion`** — ring wraps the panel header; the panel can open automatically when the model starts streaming its body
- **`DrylAiIndicator`** — companion status pill that adapts its label and pulse speed to the current state

### Wiring with `Microsoft.Extensions.AI`

```csharp
private AiState _state = AiState.None;

private async Task AskAi()
{
    _state = AiState.Thinking;
    var response = await chatClient.GetStreamingResponseAsync(prompt);

    _state = AiState.Streaming;
    await foreach (var chunk in response)
    {
        _text += chunk.Text;
        StateHasChanged();
    }

    _state = AiState.Generated;   // one-shot wash
    await Task.Delay(900);
    _state = AiState.Active;      // settle back to idle AI mode
}
```

```razor
<DrylCard Ai="@_state">
    <DrylAiIndicator State="@_state" />
    @_text
</DrylCard>

<DrylExpansion Title="AI summary" Icon="Sparkle" Ai="@_state" @bind-IsOpen="_open">
    <ChildContent>@_text</ChildContent>
</DrylExpansion>
```

The CSS primitives behind this (`.ai-aura`, `.ai-aura-ring`, `.ai-aura-glow`, `.ai-aura-wash`) live in [`dryl.css`](DRYL.Components/wwwroot/dryl.css) and can be applied to any element that isn't yet a DRYL component.

---

## Dialog & DialogService

DRYL ships a service-driven dialog system inspired by the patterns popularised by MudBlazor's `IDialogService`, but built on the DRYL glass aesthetic and **AI-native from day one**. The dialog is the natural place to host a **"human in the middle"** flow: the model proposes, the user reviews, the user approves or edits.

### Setup

```csharp
// Program.cs
builder.Services.AddDrylComponents();
```

```razor
@* App.razor or MainLayout.razor — once, at the root *@
<DrylDialogProvider />
```

### Showing a dialog

```csharp
@inject IDrylDialogService Dialogs

var reference = await Dialogs.ShowAsync<MyDialog>(
    title: "Edit profile",
    parameters: new DialogParameters { ["UserId"] = id },
    options: new DialogOptions { Size = DialogSize.Large });

var result = await reference.Result;
if (!result.Canceled)
{
    var payload = result.DataAs<MyPayload>();
}
```

### Convenience helpers

```csharp
var ok = await Dialogs.ShowConfirmAsync("Delete project?", "This cannot be undone.");
await Dialogs.ShowAlertAsync("Deployment failed", "See logs for details.");
```

### Authoring a dialog

```razor
@* MyDialog.razor — shown via IDrylDialogService *@
<DrylDialog Title="Edit profile" Ai="@_ai">
    <ChildContent>
        <DrylInputText @bind-Value="_name" Label="Name" />
    </ChildContent>
    <ActionContent>
        <DrylButton Variant="DrylButton.ButtonVariant.Ghost" @onclick="Cancel">Cancel</DrylButton>
        <DrylButton Variant="DrylButton.ButtonVariant.Primary" @onclick="Save">Save</DrylButton>
    </ActionContent>
</DrylDialog>

@code {
    [CascadingParameter] IDrylDialogInstance Instance { get; set; } = default!;
    [Parameter] public Guid UserId { get; set; }
    private string _name = "";
    private AiState _ai = AiState.None;

    void Save()   => Instance.Close(DialogResult.Ok(_name));
    void Cancel() => Instance.Cancel();
}
```

### Human in the Middle

The `Ai` parameter on `DrylDialog` walks the standard `AiState` lifecycle and the existing AI primitives carry the visual story — there is no per-component AI vocabulary. A typical wiring with `Microsoft.Extensions.AI`:

```csharp
_ai = AiState.Thinking;
await foreach (var chunk in chatClient.GetStreamingResponseAsync(prompt))
{
    if (_ai != AiState.Streaming) _ai = AiState.Streaming;
    _generated += chunk.Text;
    StateHasChanged();
}
_ai = AiState.Generated;   // one-shot reveal
// User can now edit `_generated` in a DrylTextarea and Approve / Cancel.
```

Every step is visible to the user through the dialog's border and glow — the model is at work, the model is streaming, the model is done, the user is in control.

---

## What's in the box (today)

| Component         | Category     | AI mode | Status     | Notes                                                              |
| ----------------- | ------------ | ------- | ---------- | ------------------------------------------------------------------ |
| `DrylButton`      | Actions      | ✅      | ✅ Done    | Primary / Secondary / Ghost / Danger, sizes, loading, icon slots   |
| `DrylCard`        | Surfaces     | ✅      | ✅ Done    | Glass surface, optional cursor spotlight, `Ai` state              |
| `DrylBadge`       | Data         | —       | ✅ Done    | Neutral / Accent / Success / Warning / Danger, optional dot       |
| `DrylIcon`        | Data         | —       | ✅ Done    | Lucide-based icon set, used by Button, Badge and others           |
| `DrylAiIndicator` | Intelligence | ✅      | ✅ Done    | Pulsing status pill that adapts label and speed to `AiState`      |
| `DrylInputText`   | Inputs       | ✅      | ✅ Done    | Form-bound text input with leading / trailing icon slots          |
| `DrylCheckbox`    | Inputs       | —       | ✅ Done    | Accessible checkbox with label                                    |
| `DrylSelect`      | Inputs       | —       | ✅ Done    | Styled select bound to `EditForm`                                 |
| `DrylTextarea`    | Inputs       | ✅      | ✅ Done    | Auto-resizable textarea                                           |
| `DrylToggle`      | Inputs       | —       | ✅ Done    | On/off toggle switch                                              |
| `DrylTable`       | Data         | ✅      | ✅ Done    | Generic table, sticky header, row selection, optional KPI summary bar |
| `DrylExpansion`   | Layout       | ✅      | ✅ Done    | Collapsible glass panel; stacked panels share borders and detach on open |
| `DrylLayout`      | Layout       | —       | ✅ Done    | Root shell — CSS grid with sidebar + topbar slots, cascades layout context |
| `DrylAppBar`      | Layout       | —       | ✅ Done    | Sticky top bar with optional responsive drawer-toggle hamburger |
| `DrylDrawer`      | Layout       | —       | ✅ Done    | Sidebar: always-visible column on desktop, overlay on mobile (`@bind-Open`) |
| `DrylMainContent` | Layout       | —       | ✅ Done    | Main content slot inside `DrylLayout`; handles scroll and padding |
| `DrylNavGroup`    | Layout       | —       | ✅ Done    | Labelled group of nav links inside `DrylDrawer` |
| `DrylNavLink`     | Layout       | —       | ✅ Done    | Single nav row with icon and active highlighting; supports external links |
| `DrylDialog`      | Surfaces     | ✅      | ✅ Done    | Service-driven glass dialog, focus trap, sizes, AI-aware (Human in the Middle) |
| `DrylToast`       | Surfaces     | ✅      | ✅ Done    | Service-driven toast stack; auto-dismiss, progress bar, hover-pause, 6 positions |

For the full design language, see [`DESIGN_TOKENS.md`](DESIGN_TOKENS.md) and [`COMPONENT_PATTERNS.md`](COMPONENT_PATTERNS.md).

---

## Repository layout

```
DRYL.Components/             The library (Razor Class Library, .NET 10)
  AiState.cs                 The AI state enum — shared across all AI-aware components
  Components/
    Actions/                 DrylButton
    AI/                      DrylAiIndicator (AI-specific components live here)
    Data/                    DrylBadge, DrylIcon, DrylTable, DrylTableKpi
    Inputs/                  DrylInputText, DrylCheckbox, DrylSelect, DrylTextarea, DrylToggle
    Layout/                  DrylExpansion, DrylLayout, DrylMainContent, DrylAppBar, DrylDrawer, DrylNavGroup, DrylNavLink
    Surfaces/                DrylCard, DrylDialog, DrylDialogProvider, DrylToast, DrylToastProvider
  Dialogs/                   IDrylDialogService, DialogOptions, DialogResult, DialogParameters
  Toasts/                    IDrylToastService, ToastOptions, ToastVariant, ToastPosition
  Extensions/                ServiceCollectionExtensions (AddDrylComponents)
  wwwroot/
    dryl.css                 The single stylesheet — every token, every primitive (incl. AI mode)
    js/dryl.js               Minimal JS interop (namespaced as window.dryl.*)

samples/DRYL.Components.Demo/   Sample Blazor app showing all components live
prototype/                       Original HTML prototype — visual target
CLAUDE.md                        Rules for AI agents contributing to DRYL
DESIGN_TOKENS.md                 Token reference
COMPONENT_PATTERNS.md            Component anatomy & folder conventions
```

---

## Try it locally

DRYL is not yet published to NuGet. To explore the demo app:

```bash
git clone https://github.com/Zimpi/DRYL.Components.git
cd DRYL.Components
dotnet run --project samples/DRYL.Components.Demo
```

---

## Contributing

Right now this is a solo effort, but contributions will be welcome once the core stabilizes. If you want to help:

1. Read [`CLAUDE.md`](CLAUDE.md) — the contribution rules (they apply to humans too).
2. Open an issue before starting work on a new component.
3. Every PR must respect the token system. No invented colors, no arbitrary spacings.

---

## Credits

DRYL stands on the shoulders of these open-source projects:

- **[Lucide](https://lucide.dev)** — the icon set behind `DrylIcon`. ISC-licensed. Some Lucide icons themselves derive from [Feather Icons](https://feathericons.com) (MIT, Cole Bemis).
- **[Inter](https://rsms.me/inter/)** by Rasmus Andersson — primary UI typeface. SIL Open Font License.
- **[JetBrains Mono](https://www.jetbrains.com/mono/)** — monospace typeface used for code, IDs and timestamps. SIL Open Font License.

Full license texts for bundled third-party assets are in [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md).

---

## License

MIT — see [`LICENSE`](LICENSE). Use it, fork it, ship it.
