# DRYL — Component Patterns

This file describes the **shape** a DRYL Blazor component takes. Every component in the library follows the same conventions so contributors and consumers always know what to expect.

---

## Folder layout

```
src/DRYL/
├── DRYL.csproj
├── _Imports.razor                    ← Global usings
├── AiState.cs                        ← Shared AI state enum (None / Active / Thinking / Streaming / Generated)
├── wwwroot/
│   ├── dryl.css                      ← The design system (single file, incl. AI mode primitives)
│   └── js/
│       └── dryl.js                   ← All JS interop, namespaced under `window.dryl`
├── Components/
│   ├── Actions/
│   │   ├── DrylButton.razor
│   │   ├── DrylButton.razor.css      ← Optional, only for things not in dryl.css
│   │   └── DrylIconButton.razor
│   ├── AI/
│   │   └── DrylAiIndicator.razor     ← AI-specific components live here
│   ├── Surfaces/
│   │   ├── DrylCard.razor
│   │   └── DrylModal.razor
│   ├── Inputs/
│   │   ├── DrylInputText.razor
│   │   ├── DrylInputNumber.razor
│   │   ├── DrylCheckbox.razor
│   │   └── DrylToggle.razor
│   ├── Data/
│   │   ├── DrylTable.razor
│   │   ├── DrylBadge.razor
│   │   └── DrylSparkline.razor
│   ├── Feedback/
│   │   ├── DrylAlert.razor
│   │   ├── DrylToast.razor
│   │   └── DrylTooltip.razor
│   └── Navigation/
│       ├── DrylTabs.razor
│       └── DrylBreadcrumb.razor
└── Services/
    └── ToastService.cs
```

**Where things live**
- `AiState.cs` sits at the project root — it's shared across categories, not owned by any one of them.
- `Components/AI/` is reserved for components whose **primary** purpose is to signal or shape AI activity (e.g. `DrylAiIndicator`). Components that merely *opt in* to AI mode (like `DrylCard`) stay in their semantic category.

---

## Anatomy of a DRYL component

```razor
@*  ─────────────────────────────────────────────────────────
    DrylButton — primary action component.

    Usage:
      <DrylButton Variant="ButtonVariant.Primary"
                  Loading="@isSaving"
                  OnClick="HandleSave">
        Save changes
      </DrylButton>
    ───────────────────────────────────────────────────────── *@

<button class="@CssClass"
        type="@(IsSubmit ? "submit" : "button")"
        disabled="@(Disabled || Loading)"
        aria-label="@AriaLabel"
        @onclick="HandleClick"
        @attributes="AdditionalAttributes">

    @if (Loading)
    {
        <span class="spinner" aria-hidden="true"></span>
    }
    else if (LeadingIcon is not null)
    {
        <DrylIcon Name="@LeadingIcon" Size="14" />
    }

    @ChildContent

    @if (TrailingIcon is not null && !Loading)
    {
        <DrylIcon Name="@TrailingIcon" Size="14" />
    }
</button>

@code {
    /// <summary>Visual style of the button.</summary>
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;

    /// <summary>Size of the button.</summary>
    [Parameter] public ButtonSize Size { get; set; } = ButtonSize.Medium;

    /// <summary>If true, button shows a spinner and is non-clickable.</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>If true, button is greyed out and non-clickable.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>If true, button submits the enclosing form.</summary>
    [Parameter] public bool IsSubmit { get; set; }

    /// <summary>Icon name shown before the label.</summary>
    [Parameter] public string? LeadingIcon { get; set; }

    /// <summary>Icon name shown after the label.</summary>
    [Parameter] public string? TrailingIcon { get; set; }

    /// <summary>Accessible label — required for icon-only buttons.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>Click event handler.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>Button label content.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Pass-through HTML attributes (e.g. data-*).</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClass
    {
        get
        {
            var variant = Variant switch
            {
                ButtonVariant.Primary   => "btn-primary",
                ButtonVariant.Secondary => "btn-secondary",
                ButtonVariant.Ghost     => "btn-ghost",
                ButtonVariant.Danger    => "btn-danger",
                _ => "btn-primary"
            };
            var size = Size switch
            {
                ButtonSize.Small  => "btn-sm",
                ButtonSize.Large  => "btn-lg",
                _ => ""
            };
            return $"btn {variant} {size}".Trim();
        }
    }

    private Task HandleClick(MouseEventArgs e) =>
        Disabled || Loading ? Task.CompletedTask : OnClick.InvokeAsync(e);
}
```

### Component checklist (apply to every new component)

- [ ] Razor file is named `Dryl<Noun>.razor`, lives in `Components/<Category>/`.
- [ ] Class has an XML doc comment summarizing what the component does.
- [ ] Every `[Parameter]` has an XML doc comment.
- [ ] Variants use a public `enum`. Defined alongside the component in the same file (or in `Components/<Category>/ButtonVariant.cs` if multiple components share it).
- [ ] `EventCallback<T>` is used for events. Never `Action` or `Func`.
- [ ] `RenderFragment` for slots. Use named fragments (`HeaderContent`, `FooterContent`) when the component has multiple slots.
- [ ] `[Parameter(CaptureUnmatchedValues = true)]` is included for pass-through attributes.
- [ ] All CSS classes used are defined in `dryl.css`. If not, propose adding the token, do not inline.
- [ ] Accessibility: keyboard-reachable, ARIA labels for icon-only, `:focus-visible` not overridden.
- [ ] A demo page in `samples/Pages/Demo<Component>.razor` shows every variant + size + state.

---

## Enums

Keep enums small and stable. Adding a new enum value is a minor version bump.

```csharp
public enum ButtonVariant { Primary, Secondary, Ghost, Danger }
public enum ButtonSize    { Small, Medium, Large }
public enum BadgeKind     { Neutral, Accent, Success, Warning, Danger }
public enum AlertKind     { Info, Success, Warning, Danger }
public enum InputState    { Default, Error, Success }
public enum AiState       { None, Active, Thinking, Streaming, Generated }
```

`AiState` is the single source of truth for AI activity across the library. **Never** introduce a per-component AI enum (`ChatLoadingState`, `GenerationPhase`, etc.) — every AI-aware surface must speak the same vocabulary.

---

## JS Interop

JS lives at `wwwroot/js/dryl.js`, namespaced under `window.dryl`.

```js
// wwwroot/js/dryl.js
window.dryl = window.dryl || {};

window.dryl.spotlight = {
    track(el) {
        if (!el) return;
        el.addEventListener('mousemove', (e) => {
            const r = el.getBoundingClientRect();
            el.style.setProperty('--mx', (e.clientX - r.left) + 'px');
            el.style.setProperty('--my', (e.clientY - r.top) + 'px');
        });
    }
};
```

```csharp
@inject IJSRuntime JS
@implements IAsyncDisposable

private ElementReference _el;
private IJSObjectReference? _module;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await JS.InvokeVoidAsync("dryl.spotlight.track", _el);
    }
}

public async ValueTask DisposeAsync()
{
    if (_module is not null) await _module.DisposeAsync();
}
```

Rules:
- Never call JS interop during `OnInitialized` — wait for `OnAfterRender(firstRender: true)`.
- Always namespace under `window.dryl.<feature>` — never pollute global.
- Always `IDisposable` / `IAsyncDisposable` if you attach listeners.

---

## Form-bound inputs

Inputs that participate in `EditForm` extend `InputBase<T>`:

```csharp
@inherits InputBase<string>

<input class="input @CssClass"
       value="@CurrentValue"
       @oninput="OnInput"
       @attributes="AdditionalAttributes" />

@code {
    private string CssClass => EditContext?.GetValidationMessages(FieldIdentifier).Any() == true
        ? "input-error"
        : "";

    private void OnInput(ChangeEventArgs e) =>
        CurrentValueAsString = e.Value?.ToString();

    protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
    {
        result = value ?? "";
        validationErrorMessage = "";
        return true;
    }
}
```

---

## AI-aware components

Any component whose surface can be driven by an AI (a card filled by a tool call, a textarea bound to a streaming completion, a table whose rows are populated by a model) opts in to AI mode by accepting a single parameter:

```csharp
/// <summary>AI ambient state — controls the gradient border, glow, and reveal animation.</summary>
[Parameter] public AiState Ai { get; set; } = AiState.None;
```

The render template adds the AI primitives **only when `Ai != AiState.None`** so the default cost is exactly zero — no extra DOM, no animations, no perf hit for consumers that don't use AI:

```razor
<div class="@CssClass" @attributes="AdditionalAttributes">
    @if (Ai != AiState.None)
    {
        <div class="ai-aura-ring"></div>
        <div class="ai-aura-glow"></div>
        @if (Ai == AiState.Generated)
        {
            <div class="ai-aura-wash" @key="_genTick"></div>
        }
    }
    @ChildContent
</div>
```

```csharp
private AiState _prevAi = AiState.None;
private int _genTick;

protected override void OnParametersSet()
{
    // Re-key the wash element each time we transition into Generated so the
    // one-shot animation replays on every completion.
    if (Ai == AiState.Generated && _prevAi != AiState.Generated) _genTick++;
    _prevAi = Ai;
}

private string CssClass
{
    get
    {
        var classes = new List<string> { /* …existing classes… */ };
        if (Ai != AiState.None) classes.Add("ai-aura");
        switch (Ai)
        {
            case AiState.Thinking:  classes.Add("ai-thinking");  break;
            case AiState.Streaming: classes.Add("ai-streaming"); break;
            case AiState.Generated: classes.Add("ai-generated"); break;
        }
        return string.Join(' ', classes);
    }
}
```

### Rules

- **Off by default.** `Ai` always defaults to `AiState.None`. Existing call sites must see zero change.
- **No new visuals.** Use the existing CSS primitives (`.ai-aura*`, `.ai-indicator`) verbatim. If a state needs a new look, extend `dryl.css` first — don't fork the styling inside a component.
- **Re-key on `Generated`.** The one-shot wash only re-fires if its DOM node is fresh. Use the `_genTick` pattern above (or `@key`) to force a new node on every transition into `Generated`.
- **Pair with `DrylAiIndicator`** for status feedback. Don't roll your own status pill inside the component — compose them.
- **Accessibility.** When the AI state itself is the *only* signal a screen-reader user gets that something is happening, expose it via `aria-live="polite"` on a status element (this is how `DrylAiIndicator` works). Decorative AI styling on a surface that already has its own label needs no extra ARIA.

### Streaming with `Microsoft.Extensions.AI`

The canonical lifecycle: `None → Thinking → Streaming → Generated → Active` (or back to `None` if the surface is only AI-driven transiently).

```csharp
private AiState _state = AiState.None;
private string _text = "";

private async Task AskAi(string prompt)
{
    _state = AiState.Thinking;
    var response = chatClient.GetStreamingResponseAsync(prompt);

    _state = AiState.Streaming;
    await foreach (var chunk in response)
    {
        _text += chunk.Text;
        StateHasChanged();
    }

    _state = AiState.Generated;
    await Task.Delay(900);          // let the wash play
    _state = AiState.Active;        // settle on idle AI mode (or AiState.None)
}
```

---

## Services

Cross-cutting state goes into a scoped service. Examples:

- `ToastService` — pub/sub for toast notifications
- `ModalService` — programmatically open modals from outside the markup tree
- `ThemeService` — (future) lets a host app override accent colors at runtime

```csharp
public sealed class ToastService
{
    public event Action<Toast>? OnShow;
    public void Success(string title, string? body = null) => Push(ToastKind.Success, title, body);
    public void Error(string title, string? body = null)   => Push(ToastKind.Error,   title, body);
    public void Info(string title, string? body = null)    => Push(ToastKind.Info,    title, body);

    private void Push(ToastKind k, string t, string? b) =>
        OnShow?.Invoke(new Toast(Guid.NewGuid(), k, t, b, DateTime.UtcNow));
}

public record Toast(Guid Id, ToastKind Kind, string Title, string? Body, DateTime At);

// Program.cs
builder.Services.AddScoped<ToastService>();
```

A `DrylToastHost` component subscribes once in `MainLayout.razor` and renders any active toasts.

---

## Versioning

DRYL follows **SemVer**.

- **Patch** (0.1.0 → 0.1.1) — bug fix, no API change, visual fix.
- **Minor** (0.1.0 → 0.2.0) — new component, new optional parameter, new enum value, new CSS token.
- **Major** (0.x → 1.0, 1.x → 2.0) — removed/renamed parameter, removed enum value, breaking CSS class change.

Until 1.0 is reached, minor versions may include breaking changes — but each one must be called out in `CHANGELOG.md`.
