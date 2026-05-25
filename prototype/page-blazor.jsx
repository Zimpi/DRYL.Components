/* DRYL — Blazor Patterns */

function PageBlazor() {
  return (
    <div className="fade-in">
      <div style={{ marginBottom: 48 }}>
        <div className="eyebrow" style={{ marginBottom: 12 }}>Blazor Patterns</div>
        <h1 style={{ fontSize: 44, marginBottom: 14 }}>Im Code zuhause</h1>
        <p className="lead">Die CSS-Tokens und Klassen sind so gewählt, dass sie sich natürlich in Blazor-Komponenten integrieren. Hier ein paar Patterns für den Alltag.</p>
      </div>

      <Pattern
        eyebrow="Pattern · 01"
        title="DrylButton Komponente"
        body="Eine Button-Komponente, die Variants per Enum entgegennimmt und die System-Klassen ausgibt. Klar typisiert, kein Magic-String im Markup."
      >
        <CodeBlock language="razor" code={`@code {
    public enum Variant { Primary, Secondary, Ghost, Danger }
    public enum Size { Small, Medium, Large }

    [Parameter] public Variant Kind { get; set; } = Variant.Primary;
    [Parameter] public Size SizeMode { get; set; } = Size.Medium;
    [Parameter] public bool Loading { get; set; }
    [Parameter] public string? Icon { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }

    private string Css => $"btn btn-{Kind.ToString().ToLower()} {(SizeMode == Size.Small ? "btn-sm" : SizeMode == Size.Large ? "btn-lg" : "")}";
}

<button class="@Css" disabled="@Loading" @onclick="OnClick">
    @if (Loading) { <span class="spinner"></span> }
    else if (Icon is not null) { <DrylIcon Name="@Icon" Size="14" /> }
    @ChildContent
</button>`}/>
      </Pattern>

      <Pattern
        eyebrow="Pattern · 02"
        title="Card mit Spotlight-FX"
        body="Glass-Card mit cursor-following Spotlight. Die CSS-Variablen --mx/--my werden per JSInterop gesetzt — einmal eingerichtet, gilt es für jede Card."
      >
        <CodeBlock language="razor" code={`@inject IJSRuntime JS

<div @ref="_el" class="glass-card spotlight-card" @onmousemove="OnMove">
    <div class="spotlight-fx"></div>
    <div class="spotlight-content">
        @ChildContent
    </div>
</div>

@code {
    private ElementReference _el;
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private async Task OnMove(MouseEventArgs e)
    {
        await JS.InvokeVoidAsync("dryl.setSpot", _el, e.OffsetX, e.OffsetY);
    }
}`}/>
        <div style={{ marginTop: 14 }}>
          <CodeBlock language="csharp" code={`// wwwroot/js/dryl.js
window.dryl = {
    setSpot: (el, x, y) => {
        el.style.setProperty('--mx', x + 'px');
        el.style.setProperty('--my', y + 'px');
    }
};`}/>
        </div>
      </Pattern>

      <Pattern
        eyebrow="Pattern · 03"
        title="Toast Service"
        body="Ein scoped Service plus eine OverlayHost-Komponente. Toasts erscheinen aus dem ganzen Tree — egal von welcher Page."
      >
        <CodeBlock language="csharp" code={`public sealed class ToastService
{
    public event Action<Toast>? OnShow;

    public void Success(string title, string? body = null) => Push(ToastKind.Success, title, body);
    public void Error(string title, string? body = null)   => Push(ToastKind.Error,   title, body);
    public void Info(string title, string? body = null)    => Push(ToastKind.Info,    title, body);

    private void Push(ToastKind kind, string title, string? body) =>
        OnShow?.Invoke(new Toast(Guid.NewGuid(), kind, title, body, DateTime.UtcNow));
}

// Program.cs
builder.Services.AddScoped<ToastService>();`}/>
        <div style={{ marginTop: 14 }}>
          <CodeBlock language="razor" code={`@inject ToastService Toasts

<DrylButton Kind="Variant.Primary" OnClick="@(() => Toasts.Success("Saved", "All changes are live"))">
    Save changes
</DrylButton>`}/>
        </div>
      </Pattern>

      <Pattern
        eyebrow="Pattern · 04"
        title="CSS Isolation + Tokens"
        body="Komponenten-spezifische Styles bleiben isoliert. Greifen aber immer auf die globalen Tokens zurück, statt eigene Werte zu erfinden."
      >
        <CodeBlock language="css" code={`/* Components/ServiceCard.razor.css */
.card {
    padding: var(--sp-5);
    border-radius: var(--r-lg);
    background: var(--glass-1);
    border: 1px solid var(--line);
    backdrop-filter: blur(var(--glass-blur));
    transition: border-color var(--dur-med) var(--ease-out);
}

.card:hover {
    border-color: var(--accent-line);
    box-shadow: var(--glow-accent);
}

.title {
    font-weight: 500;
    letter-spacing: -0.01em;
    color: var(--fg);
}`}/>
      </Pattern>

      <Pattern
        eyebrow="Pattern · 05"
        title="Naming Konvention"
        body="Komponenten = präzise Substantive im PascalCase mit Dryl-Präfix. CSS-Klassen = kebab-case ohne Präfix, weil die Tokens den Scope tragen."
      >
        <div style={{
          display: "grid",
          gridTemplateColumns: "1fr 1fr",
          gap: 18,
        }}>
          <div>
            <div className="eyebrow" style={{ marginBottom: 10, fontSize: 10 }}>Components</div>
            <div className="col" style={{ gap: 6, fontFamily: "var(--font-mono)", fontSize: 12.5, color: "var(--fg-muted)" }}>
              <div><span style={{ color: "var(--success)" }}>✓</span> DrylButton</div>
              <div><span style={{ color: "var(--success)" }}>✓</span> DrylCard</div>
              <div><span style={{ color: "var(--success)" }}>✓</span> DrylInputText</div>
              <div><span style={{ color: "var(--success)" }}>✓</span> DrylDataGrid</div>
              <div><span style={{ color: "var(--danger)" }}>✗</span> Button1</div>
              <div><span style={{ color: "var(--danger)" }}>✗</span> CustomTextInput</div>
            </div>
          </div>
          <div>
            <div className="eyebrow" style={{ marginBottom: 10, fontSize: 10 }}>CSS Classes</div>
            <div className="col" style={{ gap: 6, fontFamily: "var(--font-mono)", fontSize: 12.5, color: "var(--fg-muted)" }}>
              <div><span style={{ color: "var(--success)" }}>✓</span> .btn .btn-primary</div>
              <div><span style={{ color: "var(--success)" }}>✓</span> .glass-card</div>
              <div><span style={{ color: "var(--success)" }}>✓</span> .badge-success</div>
              <div><span style={{ color: "var(--success)" }}>✓</span> .field-label</div>
              <div><span style={{ color: "var(--danger)" }}>✗</span> .DrylButton</div>
              <div><span style={{ color: "var(--danger)" }}>✗</span> .greenBadge</div>
            </div>
          </div>
        </div>
      </Pattern>

      <Pattern
        eyebrow="Pattern · 06"
        title="Setup im Projekt"
        body="Drei Dateien in wwwroot, ein @import in der globalen CSS, fertig."
      >
        <CodeBlock language="razor" code={`<!-- App.razor / Layout.razor -->
<head>
    <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&family=JetBrains+Mono:wght@400;500;600&display=swap" />
    <link rel="stylesheet" href="dryl/dryl.css" />
    <link rel="stylesheet" href="DRYL.Web.styles.css" />
</head>
<body class="dryl-dark">
    <div class="aurora"><span class="orb"></span></div>
    <Routes />
    <script src="dryl/dryl.js"></script>
    <script src="_framework/blazor.web.js"></script>
</body>`}/>
      </Pattern>

      <Pattern
        eyebrow="Pattern · 07"
        title="DrylDataGrid · Header-Cell"
        body="Wenn du eigene Tabellen baust: Header-Cells lesen ihre Sort-Direction aus einem Parameter. Der Pfeil glüht in Akzentfarbe, wenn aktiv."
      >
        <CodeBlock language="razor" code={`@typeparam TItem

<th class="@HeaderCss" @onclick="ToggleSort">
    <span class="row" style="gap: 6px;">
        @Title
        @if (Active) {
            <DrylIcon Name="@(Dir == SortDir.Asc ? "ArrowUp" : "ArrowDown")"
                      Size="11" Class="active-sort-glow" />
        }
    </span>
</th>

@code {
    [Parameter] public string Title { get; set; } = "";
    [Parameter] public bool Active { get; set; }
    [Parameter] public SortDir Dir { get; set; } = SortDir.Asc;
    [Parameter] public EventCallback<SortDir> DirChanged { get; set; }

    private string HeaderCss => Active ? "header-cell active" : "header-cell";

    private Task ToggleSort() =>
        DirChanged.InvokeAsync(Dir == SortDir.Asc ? SortDir.Desc : SortDir.Asc);
}`}/>
      </Pattern>

      <div className="alert info" style={{ marginTop: 8 }}>
        <div className="ico"><Icons.Info size={14}/></div>
        <div>
          <div className="title">Drop-in ready</div>
          <div className="body">Kopiere <code>styles.css</code>, <code>Inter</code> + <code>JetBrains Mono</code>, und du hast das volle System in jedem Blazor-Projekt. Keine Build-Step, keine NPM-Tools.</div>
        </div>
      </div>
    </div>
  );
}

function Pattern({ eyebrow, title, body, children }) {
  return (
    <section className="section">
      <div className="section-head">
        <div>
          <div className="eyebrow" style={{ marginBottom: 6 }}>{eyebrow}</div>
          <h2>{title}</h2>
          <p style={{ marginTop: 8, maxWidth: "62ch" }}>{body}</p>
        </div>
      </div>
      <SpotlightCard style={{ padding: 22 }}>
        {children}
      </SpotlightCard>
    </section>
  );
}

window.PageBlazor = PageBlazor;
