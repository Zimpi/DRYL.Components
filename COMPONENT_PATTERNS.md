# DRYL — Component Patterns

This file describes the **shape** a DRYL Blazor component takes. Every component in the library follows the same conventions so contributors and consumers always know what to expect.

---

## Folder layout

```
src/DRYL/
├── DRYL.csproj
├── _Imports.razor                    ← Global usings
├── wwwroot/
│   ├── dryl.css                      ← The design system (single file)
│   └── js/
│       └── dryl.js                   ← All JS interop, namespaced under `window.dryl`
├── Components/
│   ├── Actions/
│   │   ├── DrylButton.razor
│   │   ├── DrylButton.razor.css      ← Optional, only for things not in dryl.css
│   │   └── DrylIconButton.razor
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
```

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
