# DRYL — Blazor Component Library

A dark, glassy, **AI-native** UI component library for **Blazor Server** and **Blazor WebAssembly**.

> **Status: Early development (`0.x`).** Expect breaking changes until `1.0`.

![DRYL — design system overview](https://raw.githubusercontent.com/Zimpi/DRYL.Components/main/docs/screenshots/overview.png)

## Why DRYL

- **Token-driven** — every color, spacing, radius, shadow and duration is a CSS variable. No magic numbers.
- **Light & dark** — one glass identity in two color modes; follows the system, switchable and persisted at runtime.
- **Glass surfaces** — translucent layers with `backdrop-filter`, never solid blocks.
- **AI-aware** — every interactive surface can opt into an `AiState` (`Active` / `Thinking` / `Streaming` / `Generated`), giving users one visual language for "where is the AI working".
- **No JS frameworks** — zero npm packages on top of Blazor; just CSS, Razor and minimal interop.
- **Accessible by default** — keyboard-reachable, ARIA-labeled, visible focus rings.

## Install

```bash
dotnet add package DRYL.Components
```

Supports **.NET 8, 9 and 10**.

## Quick start

**1. Register services** in `Program.cs`:

```csharp
builder.Services.AddDrylComponents();
```

**2. Reference the stylesheet** in your host page (`App.razor` / `_Host.cshtml` / `index.html`):

```html
<link rel="stylesheet" href="_content/DRYL.Components/dryl.css" />
```

**3. Add the providers** once in your root layout (for service-driven dialogs, toasts and notifications):

```razor
<DrylDialogProvider />
<DrylToastProvider />
```

**4. Use components**:

```razor
@using DRYL.Components

<DrylCard>
    <DrylButton Variant="ButtonVariant.Primary">Hello DRYL</DrylButton>
</DrylCard>
```

## Links

- **Documentation:** https://components.dryl.dev/
- **Repository:** https://github.com/Zimpi/DRYL.Components
- **Changelog:** https://github.com/Zimpi/DRYL.Components/blob/main/CHANGELOG.md
- **License:** MIT
