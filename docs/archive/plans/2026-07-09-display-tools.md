# Display Tools Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ready-made `AIFunction` display tools so an agent can answer with live DRYL components (charts, KPI stats, timeline) inline in the chat, plus a `Speed` parameter on `DrylPresence`.

**Architecture:** Tools validate & acknowledge only (no UI side effect); rendering is driven declaratively from `DrylRunBase.ToolCalls` by a new `DrylAgentAttachments` component (same lifecycle pattern as `DrylAgentToolCalls`). Shared internal payload DTOs are both the AIFunction schema source and the renderer's parse target, so "tool said shown" ⇔ "renderer renders".

**Tech Stack:** Blazor (net8/9/10 multi-target), `Microsoft.Extensions.AI` (`AIFunctionFactory`), System.Text.Json (`JsonSerializerDefaults.Web`), existing DRYL chart family / DrylStat / DrylTimeline / DrylPresence.

**Spec:** `docs/superpowers/specs/2026-07-09-display-tools-design.md`

## Global Constraints

- Tokens, not literals (CLAUDE.md §2.1) — exception: `font-size` px values are idiomatic in this codebase.
- Motion vocabulary is fixed: only `--dur-fast|med|slow`, `--ease-out|in-out|spring` (§2.5).
- AI vocabulary: only `AiState`, `.ai-aura*`; `Ai` defaults to `AiState.None` (§2.10).
- No new runtime dependencies.
- There is **no unit-test project** in this repo; per-task verification is `dotnet build`, final verification is runtime via the docs website (verify skill / Playwright).
- Branch: `feat/display-tools` (already created; spec committed).
- Versioning at the end: core `1.2.0 → 1.3.0` (MINOR), Agents `0.2.0 → 0.3.0`; CHANGELOG release cut `[1.3.0] — 2026-07-09`.
- All public types get XML doc comments.

---

### Task 1: `DrylPresence` gets a `Speed` parameter (core)

**Files:**
- Create: `DRYL.Components/PresenceSpeed.cs`
- Modify: `DRYL.Components/Components/Surfaces/DrylPresence.razor` (param + `WrapperCss`)
- Modify: `DRYL.Components/wwwroot/dryl.css` (presence block, after the transition modifier lines ~835-844)

**Interfaces:**
- Produces: `public enum PresenceSpeed { Medium, Fast, Slow }`; `DrylPresence.Speed` parameter (default `Medium`). Task 4 uses `Speed="PresenceSpeed.Slow"`.

- [ ] **Step 1: Create the enum** (`DRYL.Components/PresenceSpeed.cs`, sibling of `PresenceTransition.cs`):

```csharp
namespace DRYL.Components;

/// <summary>
/// Playback speed of a <see cref="DrylPresence"/> enter/exit animation. Each value maps
/// onto one of the fixed motion duration tokens — no new durations are introduced.
/// </summary>
public enum PresenceSpeed
{
    /// <summary>Default speed — <c>--dur-med</c> (240 ms).</summary>
    Medium,

    /// <summary>Snappy — <c>--dur-fast</c> (140 ms). For small, frequent UI.</summary>
    Fast,

    /// <summary>Deliberate — <c>--dur-slow</c> (420 ms). For content reveals the user should notice, e.g. chat attachments.</summary>
    Slow
}
```

- [ ] **Step 2: Add the parameter to `DrylPresence.razor`** — insert after the `Transition` parameter:

```csharp
    /// <summary>Playback speed of the enter/exit animation, mapped to the fixed duration tokens.</summary>
    [Parameter] public PresenceSpeed Speed { get; set; } = PresenceSpeed.Medium;
```

and in `WrapperCss`, after the `Transition` switch block, add:

```csharp
            if (Speed == PresenceSpeed.Fast) parts.Add("presence--fast");
            else if (Speed == PresenceSpeed.Slow) parts.Add("presence--slow");
```

- [ ] **Step 3: Add the CSS** in `dryl.css` directly after the `.presence--slide-right.presence-exit` line:

```css
/* Speed modifiers — remap the animation onto the other fixed duration tokens. */
.presence--fast.presence-enter, .presence--fast.presence-exit { animation-duration: var(--dur-fast); }
.presence--slow.presence-enter, .presence--slow.presence-exit { animation-duration: var(--dur-slow); }
```

(`prefers-reduced-motion` already neutralises `.presence-enter/.presence-exit` below — nothing to add.)

- [ ] **Step 4: Build**

Run: `dotnet build DRYL.Components/DRYL.Components.csproj`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/PresenceSpeed.cs DRYL.Components/Components/Surfaces/DrylPresence.razor DRYL.Components/wwwroot/dryl.css
git commit -m "feat(presence): Speed parameter (Medium/Fast/Slow) on the fixed duration tokens"
```

---

### Task 2: Display payload specs + shared validation (Agents)

**Files:**
- Create: `DRYL.Components.Agents/Tools/DisplaySpecs.cs`

**Interfaces:**
- Produces (all `internal`, namespace `DRYL.Components.Agents.Tools`):
  - `static class DisplayToolNames` — consts `LineChart = "show_line_chart"`, `AreaChart = "show_area_chart"`, `BarChart = "show_bar_chart"`, `DonutChart = "show_donut_chart"`, `Stats = "show_stats"`, `Timeline = "show_timeline"`; `bool IsDisplayTool(string name)`.
  - `static class DisplayJson` — `JsonSerializerOptions Options` (Web defaults); `bool TryParse<T>(string? json, out T? value) where T : class`.
  - DTOs with `Validate()` returning `null` or a model-facing error sentence: `CartesianChartArgs`, `DonutChartArgs`, `StatsArgs`, `TimelineArgs`; item specs `ChartSeriesSpec`, `ChartSegmentSpec`, `StatSpec` (with `DeltaDirection ParsedDirection`), `TimelineEventSpec` (with `TimelineVariant ParsedVariant`).

- [ ] **Step 1: Write `DisplaySpecs.cs`** (complete file):

```csharp
using System.ComponentModel;
using System.Text.Json;

namespace DRYL.Components.Agents.Tools;

/// <summary>The tool names of the ready-made display tools (see <see cref="DrylDisplayTools"/>).</summary>
internal static class DisplayToolNames
{
    public const string LineChart  = "show_line_chart";
    public const string AreaChart  = "show_area_chart";
    public const string BarChart   = "show_bar_chart";
    public const string DonutChart = "show_donut_chart";
    public const string Stats      = "show_stats";
    public const string Timeline   = "show_timeline";

    public static bool IsDisplayTool(string name) => name is
        LineChart or AreaChart or BarChart or DonutChart or Stats or Timeline;
}

/// <summary>Shared JSON handling for display tool arguments (camelCase, case-insensitive).</summary>
internal static class DisplayJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Tolerant parse of a tool-call argument JSON object; false on malformed input.</summary>
    public static bool TryParse<T>(string? json, out T? value) where T : class
    {
        value = null;
        if (string.IsNullOrWhiteSpace(json)) return false;
        try { value = JsonSerializer.Deserialize<T>(json, Options); }
        catch (JsonException) { return false; }
        return value is not null;
    }
}

/// <summary>One data series for the cartesian display tools.</summary>
internal sealed class ChartSeriesSpec
{
    [Description("Series name shown in the legend and tooltips.")]
    public string Name { get; set; } = string.Empty;

    [Description("The series values — exactly one number per category label, in label order.")]
    public IReadOnlyList<double>? Data { get; set; }
}

/// <summary>Arguments of the line / area / bar chart tools.</summary>
internal sealed class CartesianChartArgs
{
    public string? Title { get; set; }
    public IReadOnlyList<string>? Labels { get; set; }
    public IReadOnlyList<ChartSeriesSpec>? Series { get; set; }
    public string? ValueFormat { get; set; }
    public bool Stacked { get; set; }

    /// <summary>Null when valid; otherwise a corrective, model-facing error sentence.</summary>
    public string? Validate()
    {
        if (Labels is null || Labels.Count == 0)
            return "labels must contain at least one category label.";
        if (Series is null || Series.Count == 0)
            return "series must contain at least one series.";
        if (Series.Count > 6)
            return "at most 6 series are supported — aggregate the rest.";
        foreach (var s in Series)
        {
            if (string.IsNullOrWhiteSpace(s.Name))
                return "every series needs a non-empty name.";
            if (s.Data is null || s.Data.Count == 0)
                return $"series '{s.Name}' has no data values.";
            if (s.Data.Count != Labels.Count)
                return $"series '{s.Name}' has {s.Data.Count} values but there are {Labels.Count} labels — they must match 1:1.";
        }
        return ValidateFormat(ValueFormat);
    }

    internal static string? ValidateFormat(string? format)
    {
        if (format is null) return null;
        try { _ = 0d.ToString(format); return null; }
        catch (FormatException)
        {
            return $"valueFormat '{format}' is not a valid .NET numeric format string — use e.g. 'N0', 'C0' or '0.0'.";
        }
    }
}

/// <summary>One segment of the donut chart tool.</summary>
internal sealed class ChartSegmentSpec
{
    [Description("Segment name shown in the legend and tooltip.")]
    public string Label { get; set; } = string.Empty;

    [Description("Segment value; its share of the total drives the angle. Must be greater than 0.")]
    public double Value { get; set; }
}

/// <summary>Arguments of the donut chart tool.</summary>
internal sealed class DonutChartArgs
{
    public string? Title { get; set; }
    public IReadOnlyList<ChartSegmentSpec>? Segments { get; set; }
    public string? ValueFormat { get; set; }

    public string? Validate()
    {
        if (Segments is null || Segments.Count == 0)
            return "segments must contain at least one segment.";
        if (Segments.Count > 6)
            return "at most 6 segments are supported — aggregate the rest into an 'Other' segment.";
        foreach (var s in Segments)
        {
            if (string.IsNullOrWhiteSpace(s.Label))
                return "every segment needs a non-empty label.";
            if (s.Value <= 0 || double.IsNaN(s.Value) || double.IsInfinity(s.Value))
                return $"segment '{s.Label}' must have a value greater than 0.";
        }
        return CartesianChartArgs.ValidateFormat(ValueFormat);
    }
}

/// <summary>One KPI card of the stats tool.</summary>
internal sealed class StatSpec
{
    [Description("Short metric label, e.g. 'Revenue'.")]
    public string Label { get; set; } = string.Empty;

    [Description("The headline value, pre-formatted as text, e.g. '€184k'.")]
    public string Value { get; set; } = string.Empty;

    [Description("Optional change indicator text, e.g. '+12.4%'.")]
    public string? Delta { get; set; }

    [Description("Trend direction of the delta: 'up', 'down' or 'neutral'.")]
    public string? Direction { get; set; }

    internal DeltaDirection ParsedDirection => Direction?.ToLowerInvariant() switch
    {
        "up" => DeltaDirection.Up,
        "down" => DeltaDirection.Down,
        "neutral" => DeltaDirection.Neutral,
        _ => DeltaDirection.None,
    };
}

/// <summary>Arguments of the stats tool.</summary>
internal sealed class StatsArgs
{
    public IReadOnlyList<StatSpec>? Stats { get; set; }

    public string? Validate()
    {
        if (Stats is null || Stats.Count == 0)
            return "stats must contain at least one entry.";
        if (Stats.Count > 6)
            return "at most 6 stats are supported — pick the most important ones.";
        foreach (var s in Stats)
        {
            if (string.IsNullOrWhiteSpace(s.Label) || string.IsNullOrWhiteSpace(s.Value))
                return "every stat needs a non-empty label and value.";
            if (s.Direction is not null and not ("up" or "down" or "neutral"))
                return $"direction '{s.Direction}' is invalid — use 'up', 'down' or 'neutral'.";
        }
        return null;
    }
}

/// <summary>One event of the timeline tool.</summary>
internal sealed class TimelineEventSpec
{
    [Description("Title line of the event.")]
    public string Title { get; set; } = string.Empty;

    [Description("Optional pre-formatted timestamp, e.g. '09:24' or 'May 12'.")]
    public string? Timestamp { get; set; }

    [Description("Optional body text below the title.")]
    public string? Text { get; set; }

    [Description("Optional marker tint: 'default', 'success', 'warning' or 'danger'.")]
    public string? Kind { get; set; }

    internal TimelineVariant ParsedVariant => Kind?.ToLowerInvariant() switch
    {
        "success" => TimelineVariant.Success,
        "warning" => TimelineVariant.Warning,
        "danger" => TimelineVariant.Danger,
        _ => TimelineVariant.Default,
    };
}

/// <summary>Arguments of the timeline tool.</summary>
internal sealed class TimelineArgs
{
    public string? Title { get; set; }
    public IReadOnlyList<TimelineEventSpec>? Events { get; set; }

    public string? Validate()
    {
        if (Events is null || Events.Count == 0)
            return "events must contain at least one event.";
        foreach (var e in Events)
        {
            if (string.IsNullOrWhiteSpace(e.Title))
                return "every event needs a non-empty title.";
            if (e.Kind is not null and not ("default" or "success" or "warning" or "danger"))
                return $"kind '{e.Kind}' is invalid — use 'default', 'success', 'warning' or 'danger'.";
        }
        return null;
    }
}
```

Note: `TimelineVariant` / `DeltaDirection` live in the core `DRYL.Components` namespace (already imported via the project reference). If `s.Direction is not null and not (...)` pattern syntax fights the compiler on a target framework, fall back to an explicit `&&` chain — behaviour over cleverness.

- [ ] **Step 2: Build**

Run: `dotnet build DRYL.Components.Agents/DRYL.Components.Agents.csproj`
Expected: Build succeeded (all three TFMs).

- [ ] **Step 3: Commit**

```bash
git add DRYL.Components.Agents/Tools/DisplaySpecs.cs
git commit -m "feat(agents): typed payload specs + shared validation for the display tools"
```

---

### Task 3: `DrylDisplayTools` — the tool set

**Files:**
- Create: `DRYL.Components.Agents/Tools/DrylDisplayTools.cs`

**Interfaces:**
- Consumes: `DisplayToolNames`, the args DTOs and their `Validate()` from Task 2.
- Produces: `public sealed class DrylDisplayTools` with `static DrylDisplayTools Create()`, `AITool` properties `LineChart`, `AreaChart`, `BarChart`, `DonutChart`, `Stats`, `Timeline`, and `IList<AITool> All`.

- [ ] **Step 1: Write `DrylDisplayTools.cs`** (complete file):

```csharp
using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Agents.Tools;

/// <summary>
/// Ready-made display tool functions for the Microsoft Agent Framework: hand them to your
/// agent and the model can answer with live DRYL components — charts, KPI stats and a
/// timeline — inline in the conversation. The tools themselves only validate and acknowledge;
/// rendering is done by <c>DrylAgentAttachments</c> from the run's tool-call trace, so they
/// work with <c>Start</c>, <c>Replay</c> and the multi-agent orchestrations alike.
/// </summary>
public sealed class DrylDisplayTools
{
    private DrylDisplayTools()
    {
        LineChart = AIFunctionFactory.Create(ShowLineChartImpl, DisplayToolNames.LineChart,
            "Show the user a live line chart, rendered inline in the conversation. " +
            "Use it for trends over ordered categories (e.g. months). Prefer this over describing numbers in text.");
        AreaChart = AIFunctionFactory.Create(ShowAreaChartImpl, DisplayToolNames.AreaChart,
            "Show the user a live area chart (line with gradient fill), rendered inline in the conversation. " +
            "Use it for cumulative or volume-like trends. Prefer this over describing numbers in text.");
        BarChart = AIFunctionFactory.Create(ShowBarChartImpl, DisplayToolNames.BarChart,
            "Show the user a live bar chart, rendered inline in the conversation. " +
            "Use it to compare discrete categories; set stacked=true for part-of-whole comparisons.");
        DonutChart = AIFunctionFactory.Create(ShowDonutChartImpl, DisplayToolNames.DonutChart,
            "Show the user a live donut chart, rendered inline in the conversation. " +
            "Use it for share-of-total breakdowns with up to 6 segments.");
        Stats = AIFunctionFactory.Create(ShowStatsImpl, DisplayToolNames.Stats,
            "Show the user a row of KPI stat cards (label, big value, optional delta with direction), " +
            "rendered inline in the conversation. Use it for headline metrics instead of listing them as text.");
        Timeline = AIFunctionFactory.Create(ShowTimelineImpl, DisplayToolNames.Timeline,
            "Show the user a vertical timeline of events (title, optional timestamp/text/kind), " +
            "rendered inline in the conversation. Use it for sequences, histories and step-by-step progress.");
        All = new List<AITool> { LineChart, AreaChart, BarChart, DonutChart, Stats, Timeline };
    }

    /// <summary>Create the display tool set. No dependencies — safe anywhere a run is started.</summary>
    public static DrylDisplayTools Create() => new();

    /// <summary>Line chart tool (<c>show_line_chart</c> → <c>DrylLineChart</c>).</summary>
    public AITool LineChart { get; }

    /// <summary>Area chart tool (<c>show_area_chart</c> → <c>DrylAreaChart</c>).</summary>
    public AITool AreaChart { get; }

    /// <summary>Bar chart tool (<c>show_bar_chart</c> → <c>DrylBarChart</c>).</summary>
    public AITool BarChart { get; }

    /// <summary>Donut chart tool (<c>show_donut_chart</c> → <c>DrylDonutChart</c>).</summary>
    public AITool DonutChart { get; }

    /// <summary>KPI stats tool (<c>show_stats</c> → a row of <c>DrylStat</c> cards).</summary>
    public AITool Stats { get; }

    /// <summary>Timeline tool (<c>show_timeline</c> → <c>DrylTimeline</c>).</summary>
    public AITool Timeline { get; }

    /// <summary>All six display tools — hand straight to the agent.</summary>
    public IList<AITool> All { get; }

    private static string ShowLineChartImpl(
        [Description("Category labels for the x-axis, in order, e.g. months.")] string[] labels,
        [Description("One or more data series; each needs exactly one value per label.")] ChartSeriesSpec[] series,
        [Description("Optional short heading shown above the chart.")] string? title = null,
        [Description("Optional .NET numeric format string for values, e.g. 'N0' or 'C0'.")] string? valueFormat = null)
        => Ack(new CartesianChartArgs { Title = title, Labels = labels, Series = series, ValueFormat = valueFormat }
            .Validate(), "line chart");

    private static string ShowAreaChartImpl(
        [Description("Category labels for the x-axis, in order, e.g. months.")] string[] labels,
        [Description("One or more data series; each needs exactly one value per label.")] ChartSeriesSpec[] series,
        [Description("Optional short heading shown above the chart.")] string? title = null,
        [Description("Optional .NET numeric format string for values, e.g. 'N0' or 'C0'.")] string? valueFormat = null)
        => Ack(new CartesianChartArgs { Title = title, Labels = labels, Series = series, ValueFormat = valueFormat }
            .Validate(), "area chart");

    private static string ShowBarChartImpl(
        [Description("Category labels for the x-axis, in order.")] string[] labels,
        [Description("One or more data series; each needs exactly one value per label.")] ChartSeriesSpec[] series,
        [Description("Optional short heading shown above the chart.")] string? title = null,
        [Description("Stack the series into one bar per category (part-of-whole). Negative values are not supported when stacked.")] bool stacked = false,
        [Description("Optional .NET numeric format string for values, e.g. 'N0' or 'C0'.")] string? valueFormat = null)
        => Ack(new CartesianChartArgs { Title = title, Labels = labels, Series = series, ValueFormat = valueFormat, Stacked = stacked }
            .Validate(), "bar chart");

    private static string ShowDonutChartImpl(
        [Description("The segments (1–6); each value's share of the total drives its angle.")] ChartSegmentSpec[] segments,
        [Description("Optional short heading shown above the chart.")] string? title = null,
        [Description("Optional .NET numeric format string for values, e.g. 'N0' or 'C0'.")] string? valueFormat = null)
        => Ack(new DonutChartArgs { Title = title, Segments = segments, ValueFormat = valueFormat }
            .Validate(), "donut chart");

    private static string ShowStatsImpl(
        [Description("The KPI cards to show (1–6), most important first.")] StatSpec[] stats)
        => Ack(new StatsArgs { Stats = stats }.Validate(), "KPI stats");

    private static string ShowTimelineImpl(
        [Description("The events, in display order (top to bottom).")] TimelineEventSpec[] events,
        [Description("Optional short heading shown above the timeline.")] string? title = null)
        => Ack(new TimelineArgs { Title = title, Events = events }.Validate(), "timeline");

    private static string Ack(string? error, string what) =>
        error is null
            ? $"The {what} is now shown to the user inline in the conversation. Do not repeat its data as text."
            : $"NOT shown to the user — {error} Fix the arguments and call the tool again.";
}
```

- [ ] **Step 2: Build**

Run: `dotnet build DRYL.Components.Agents/DRYL.Components.Agents.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add DRYL.Components.Agents/Tools/DrylDisplayTools.cs
git commit -m "feat(agents): DrylDisplayTools — six ready-made display AIFunction tools"
```

---

### Task 4: `DrylAgentAttachments` — the renderer

**Files:**
- Create: `DRYL.Components.Agents/Display/DrylAgentAttachments.razor`
- Create: `DRYL.Components.Agents/Display/DrylAgentAttachments.razor.css`

**Interfaces:**
- Consumes: `DisplayToolNames`, `DisplayJson.TryParse`, args DTOs (Task 2); `DrylRunBase.ToolCalls`/`OnChange`; core `ChartSeries`, `ChartSegment`, `DrylPresence` + `PresenceSpeed.Slow` (Task 1).
- Produces: `DrylAgentAttachments` component, namespace `DRYL.Components.Agents`, parameters `Run` (`DrylRunBase?`) and `Class` (`string?`).

- [ ] **Step 1: Write `DrylAgentAttachments.razor`** (complete file):

```razor
@namespace DRYL.Components.Agents
@using DRYL.Components.Agents.Tools
@implements IDisposable

@*  ─────────────────────────────────────────────────────────
    DrylAgentAttachments — renders a run's display-tool calls
    (DrylDisplayTools) as live DRYL components inline in the chat.

    Each successfully validated call becomes an "attachment" that
    glides in via DrylPresence; invalid calls render nothing (the
    model already received a corrective tool result).

    Usage — under the streaming answer text:
      <DrylMessage Role="MessageRole.Assistant" Ai="@_run.State">
          <DrylAiStream Source="_run.TextStream">
              <DrylMarkdown Content="@context.Text" Ai="@context.State" />
          </DrylAiStream>
          <DrylAgentAttachments Run="_run" />
      </DrylMessage>
    ───────────────────────────────────────────────────────── *@

@if (Run is not null)
{
    var attachments = Visible.ToList();
    @if (attachments.Count > 0)
    {
        <div class="@RootCssClass">
            @foreach (var a in attachments)
            {
                <DrylPresence @key="a.CallId" Visible="true" Appear
                              Transition="PresenceTransition.SlideUp"
                              Speed="PresenceSpeed.Slow">
                    <div class="agent-attachment">
                        @if (!string.IsNullOrWhiteSpace(a.Title))
                        {
                            <div class="agent-attachment-title">@a.Title</div>
                        }
                        @a.Body
                    </div>
                </DrylPresence>
            }
        </div>
    }
}

@code {
    /// <summary>The agent run whose display-tool calls to render.</summary>
    [Parameter] public DrylRunBase? Run { get; set; }

    /// <summary>Extra CSS class(es) merged onto the container's own classes.</summary>
    [Parameter] public string? Class { get; set; }

    private readonly Dictionary<string, Attachment?> _cache = new();
    private DrylRunBase? _subscribed;

    private sealed record Attachment(string CallId, string? Title, RenderFragment Body);

    private IEnumerable<Attachment> Visible
    {
        get
        {
            if (Run is null) yield break;
            foreach (var call in Run.ToolCalls)
            {
                if (!DisplayToolNames.IsDisplayTool(call.ToolName)) continue;
                if (!_cache.TryGetValue(call.CallId, out var attachment))
                {
                    attachment = BuildAttachment(call);
                    _cache[call.CallId] = attachment;
                }
                if (attachment is not null) yield return attachment;
            }
        }
    }

    // Parses + validates the call's arguments with the same code path the tool used,
    // so an attachment renders exactly when the model was told "shown to the user".
    private Attachment? BuildAttachment(DrylToolInvocation call)
    {
        switch (call.ToolName)
        {
            case DisplayToolNames.LineChart:
            case DisplayToolNames.AreaChart:
            case DisplayToolNames.BarChart:
                if (!DisplayJson.TryParse<CartesianChartArgs>(call.Arguments, out var c) || c!.Validate() is not null)
                    return null;
                return new Attachment(call.CallId, c.Title, CartesianChart(call.ToolName, c));

            case DisplayToolNames.DonutChart:
                if (!DisplayJson.TryParse<DonutChartArgs>(call.Arguments, out var d) || d!.Validate() is not null)
                    return null;
                return new Attachment(call.CallId, d.Title, Donut(d));

            case DisplayToolNames.Stats:
                if (!DisplayJson.TryParse<StatsArgs>(call.Arguments, out var s) || s!.Validate() is not null)
                    return null;
                return new Attachment(call.CallId, null, StatRow(s));

            case DisplayToolNames.Timeline:
                if (!DisplayJson.TryParse<TimelineArgs>(call.Arguments, out var t) || t!.Validate() is not null)
                    return null;
                return new Attachment(call.CallId, t.Title, Timeline(t));

            default:
                return null;
        }
    }

    private static IReadOnlyList<ChartSeries> ToSeries(CartesianChartArgs args) =>
        args.Series!.Select(s => new ChartSeries(s.Name, s.Data!)).ToList();

    private RenderFragment CartesianChart(string toolName, CartesianChartArgs a) => __builder =>
    {
        switch (toolName)
        {
            case DisplayToolNames.LineChart:
                <DrylLineChart Labels="@a.Labels" Series="@ToSeries(a)" ValueFormat="@a.ValueFormat"
                               Smooth ShowMarkers Ai="AiState.Generated" />
                break;
            case DisplayToolNames.AreaChart:
                <DrylAreaChart Labels="@a.Labels" Series="@ToSeries(a)" ValueFormat="@a.ValueFormat"
                               Smooth Ai="AiState.Generated" />
                break;
            case DisplayToolNames.BarChart:
                <DrylBarChart Labels="@a.Labels" Series="@ToSeries(a)" ValueFormat="@a.ValueFormat"
                              Stacked="@a.Stacked" Ai="AiState.Generated" />
                break;
        }
    };

    private RenderFragment Donut(DonutChartArgs a) => __builder =>
    {
        var segments = a.Segments!.Select(s => new ChartSegment(s.Label, s.Value)).ToList();
        <DrylDonutChart Segments="@segments" ValueFormat="@a.ValueFormat" Ai="AiState.Generated" />
    };

    private RenderFragment StatRow(StatsArgs a) => __builder =>
    {
        <div class="agent-attachment-stats">
            @foreach (var s in a.Stats!)
            {
                <DrylStat Label="@s.Label" Value="@s.Value" Delta="@s.Delta"
                          Direction="@s.ParsedDirection" Ai="AiState.Generated" />
            }
        </div>
    };

    private RenderFragment Timeline(TimelineArgs a) => __builder =>
    {
        <DrylTimeline>
            @foreach (var e in a.Events!)
            {
                @if (string.IsNullOrWhiteSpace(e.Text))
                {
                    <DrylTimelineItem Title="@e.Title" Timestamp="@e.Timestamp" Variant="@e.ParsedVariant" />
                }
                else
                {
                    <DrylTimelineItem Title="@e.Title" Timestamp="@e.Timestamp" Variant="@e.ParsedVariant">
                        @e.Text
                    </DrylTimelineItem>
                }
            }
        </DrylTimeline>
    };

    private string RootCssClass =>
        string.IsNullOrWhiteSpace(Class) ? "agent-attachments" : $"agent-attachments {Class}";

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_subscribed, Run)) return;
        if (_subscribed is not null) _subscribed.OnChange -= HandleChange;
        _subscribed = Run;
        _cache.Clear();
        if (_subscribed is not null) _subscribed.OnChange += HandleChange;
    }

    private void HandleChange() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (_subscribed is not null) _subscribed.OnChange -= HandleChange;
    }
}
```

Implementation notes:
- Timeline items deliberately get **no** `Ai` (a Generated wash on every tiny marker is noise; the attachment-level presence already marks arrival). Charts/stats get `Ai="AiState.Generated"` per spec.
- If the Razor compiler rejects markup inside `switch` in `CartesianChart`, split into three tiny fragments (`Line(a)`, `Area(a)`, `Bar(a)`) chosen in `BuildAttachment` — same content.

- [ ] **Step 2: Write `DrylAgentAttachments.razor.css`**:

```css
.agent-attachments {
    display: flex;
    flex-direction: column;
    gap: var(--sp-3);
    margin-top: var(--sp-3);
}

.agent-attachment {
    display: flex;
    flex-direction: column;
    gap: var(--sp-2);
    min-width: 0;
}

.agent-attachment-title {
    font-size: 13px;
    font-weight: 600;
    color: var(--fg-muted);
}

.agent-attachment-stats {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
    gap: var(--sp-3);
}
```

- [ ] **Step 3: Build**

Run: `dotnet build DRYL.Components.Agents/DRYL.Components.Agents.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add DRYL.Components.Agents/Display/
git commit -m "feat(agents): DrylAgentAttachments — render display-tool calls as live components"
```

---

### Task 5: Website demo (analyst chat) + catalog update

**Files:**
- Create: `c:/Users/janzi/Desktop/DRYL/DRYL.Website/Components/Examples/Agents/DisplayTools.razor`
- Modify: `c:/Users/janzi/Desktop/DRYL/DRYL.Website/Components/Examples/Agents/_Simulation/SimScenarios.cs` (add `AnalystAgent()` as new Section after ToolDemoAgent)
- Modify: `c:/Users/janzi/Desktop/DRYL/DRYL.Website/Components/Pages/DemoAgents.razor` (new `DemoExample`, header version `0.1.0` → `0.3.0`)
- Modify: `c:/Users/janzi/Desktop/DRYL/DRYL.Website/Components/ComponentCatalog.cs:141` (Agents entry description)

**Interfaces:**
- Consumes: `DrylDisplayTools.Create()`, `DrylAgentAttachments`, `SimulatedChatClient`, `SimTurn`/`SimStep`, `DrylAgentRunner.Start`.

- [ ] **Step 1: Add `AnalystAgent()` to `SimScenarios.cs`** (new section between ToolDemoAgent and BuildDemoAgent; needs no dialog service):

```csharp
    // ── Section 3b — Display tools (the model answers with components) ──────────────────────────

    /// <summary>
    /// An agent armed with the six display tools, scripted to answer a sales question with
    /// KPI stats, a line chart and a donut chart inline in the chat. The tool calls are real
    /// (the framework invokes DrylDisplayTools); only the model is scripted.
    /// </summary>
    public static AIAgent AnalystAgent()
    {
        var display = DrylDisplayTools.Create();
        var turns = new List<SimTurn>
        {
            SimTurn.Of(
                SimStep.Say("Q2 in one view — headline numbers first:\n\n"),
                SimStep.Call("show_stats", SimStep.Args(new
                {
                    stats = new object[]
                    {
                        new { label = "Revenue", value = "€184k", delta = "+12.4%", direction = "up" },
                        new { label = "New customers", value = "312", delta = "+8.1%", direction = "up" },
                        new { label = "Churn", value = "2.1%", delta = "-0.4 pp", direction = "down" },
                    },
                }))),
            SimTurn.Of(SimStep.Call("show_line_chart", SimStep.Args(new
            {
                title = "Revenue vs. plan (Q2, k€)",
                labels = new[] { "Apr", "May", "Jun" },
                series = new object[]
                {
                    new { name = "Revenue", data = new[] { 54.0, 61.0, 69.0 } },
                    new { name = "Plan", data = new[] { 52.0, 58.0, 64.0 } },
                },
                valueFormat = "N0",
            }))),
            SimTurn.Of(SimStep.Call("show_donut_chart", SimStep.Args(new
            {
                title = "Revenue by region (k€)",
                segments = new object[]
                {
                    new { label = "EU", value = 98.0 },
                    new { label = "US", value = 61.0 },
                    new { label = "APAC", value = 25.0 },
                },
            }))),
            SimTurn.Of(SimStep.Say(
                "Bottom line: Q2 closed **7.8% above plan**. The EU push is doing the heavy " +
                "lifting, June set a new monthly record, and churn keeps trending down.")),
        };

        return new ChatClientAgent(
            new SimulatedChatClient(turns),
            instructions: "You are a data analyst. Prefer the display tools over prose for data.",
            name: "Analyst",
            description: null,
            tools: display.All);
    }
```

- [ ] **Step 2: Create `DisplayTools.razor`**:

```razor
@using DRYL.Website.Components.Examples.Agents.Simulation
@inject DrylAgentRunner Runner

<DrylChat Height="520px">
    <DrylMessage Role="MessageRole.User" Author="You">How did Q2 go?</DrylMessage>

    @if (_run is not null)
    {
        <DrylMessage Role="MessageRole.Assistant" Author="Analyst" AvatarIcon="Sparkle" Ai="@_run.State">
            <DrylAiStream Source="_run.TextStream">
                <DrylMarkdown Content="@context.Text" Ai="@context.State" />
            </DrylAiStream>
            <DrylAgentAttachments Run="_run" />
        </DrylMessage>
    }

    <Footer>
        <DrylButton OnClick="Start" Disabled="_run is not null" Icon="Sparkle">
            Ask the analyst
        </DrylButton>
    </Footer>
</DrylChat>

@code {
    private DrylAgentRun? _run;

    private async Task Start()
    {
        // The six DrylDisplayTools are handed to the agent; the framework invokes them for
        // real, and DrylAgentAttachments renders each call as a live component in the chat.
        var agent = SimScenarios.AnalystAgent();
        var session = await agent.CreateSessionAsync();
        _run = Runner.Start(agent, session, "How did Q2 go?");
    }
}
```

(Check `DrylAiStream`'s parameter list while implementing: if `Key` is required, omit is fine — `HumanTools.razor` passes it only because it uses a scope. Check `DrylMessage` really exposes `AvatarIcon` — the doc header says it does; if the parameter is named differently, follow the component.)

- [ ] **Step 3: Register the demo in `DemoAgents.razor`** — after the StructuredGeneration `DemoExample`, insert:

```razor
    <DemoExample Title="Display tools — the AI answers with components" Source="Agents/DisplayTools"
                 Description="The mirror image of the human-in-the-loop tools: six ready-made display AIFunctions (line/area/bar/donut chart, KPI stats, timeline). Hand them to the agent and the model answers with live DRYL components inline in the chat — DrylAgentAttachments picks every validated call off the run's tool trace and glides it in. Invalid arguments never render; the model gets a corrective tool result and retries.">
        <DRYL.Website.Components.Examples.Agents.DisplayTools />
    </DemoExample>
```

and in the `ComponentDocHeader` change `(<code>0.1.0</code>)` to `(<code>0.3.0</code>)`.

- [ ] **Step 4: Update the catalog entry** in `ComponentCatalog.cs` — replace the Agents entry description:

```csharp
        new("Agents",      "agents",      "Intelligence", null,              "",   true, "Companion lib — Agent Framework → AiState, structured, display tools.","Server",
            SourceUrlOverride: $"{RepoUrl}/tree/main/DRYL.Components.Agents"),
```

- [ ] **Step 5: Build the website**

Run: `dotnet build "c:/Users/janzi/Desktop/DRYL/DRYL.Website/DRYL.Website.csproj"`
Expected: Build succeeded.

- [ ] **Step 6: Commit** (website repo is part of the same working tree set; commit in its repo if separate, otherwise together — follow the existing repo layout)

```bash
git add Components/Examples/Agents/DisplayTools.razor Components/Examples/Agents/_Simulation/SimScenarios.cs Components/Pages/DemoAgents.razor Components/ComponentCatalog.cs
git commit -m "docs(website): display-tools demo — analyst chat with inline components"
```

---

### Task 6: Docs & versioning

**Files:**
- Modify: `CHANGELOG.md` (cut `[1.3.0] — 2026-07-09`)
- Modify: `DRYL.Components/DRYL.Components.csproj:8` (`<Version>1.2.0</Version>` → `1.3.0`)
- Modify: `DRYL.Components.Agents/DRYL.Components.Agents.csproj` (`<Version>0.2.0</Version>` → `0.3.0`, `<PackageReleaseNotes>` → `Experimental 0.3.0. See CHANGELOG.md.`, `<Description>` gains display tools)
- Modify: `DRYL.Components.Agents/PACKAGE.md` (version note, new display-tools section)

- [ ] **Step 1: CHANGELOG** — replace the empty `## [Unreleased]` block with:

```markdown
## [Unreleased]

## [1.3.0] — 2026-07-09

### Added
- `DrylPresence` — New `Speed` parameter (`PresenceSpeed`: Medium / Fast / Slow) remaps the enter/exit animation onto the fixed duration tokens; default Medium is pixel-identical to before
- `DrylDisplayTools` — (Agents) Factory for six ready-made display `AIFunction` tools (`show_line_chart`, `show_area_chart`, `show_bar_chart`, `show_donut_chart`, `show_stats`, `show_timeline`); tools validate against small typed schemas and return corrective, model-facing errors so the model can retry
- `DrylAgentAttachments` — (Agents) Renders a run's display-tool calls as live DRYL components (charts, KPI stat row, timeline) inline in the chat; each validated attachment glides in via `DrylPresence` (Slow) with the shared Generated reveal
```

- [ ] **Step 2: csproj bumps** as listed above (core 1.3.0; Agents 0.3.0 + release notes; extend the Agents `<Description>` with `..., and ready-made display tools that let the model answer with live DRYL components inline in the chat (DrylDisplayTools + DrylAgentAttachments).`).

- [ ] **Step 3: PACKAGE.md** — update the experimental version line to `**Experimental — 0.3.0.**`, and insert a new section after section 3:

```markdown
## 4 — Display tools (the model answers with components)

The mirror image of the human-in-the-loop tools: six ready-made display `AIFunction`s
(`show_line_chart`, `show_area_chart`, `show_bar_chart`, `show_donut_chart`, `show_stats`,
`show_timeline`). Hand them to the agent; the model can answer with live DRYL components
inline in the conversation. The tools only validate and acknowledge — rendering is driven
from the run's tool-call trace by `DrylAgentAttachments`, so they work with `Start`,
`Replay` and the orchestrations alike. Invalid arguments never render; the model receives
a corrective error string and retries.

```csharp
var display = DrylDisplayTools.Create();          // no dependencies
var agent = new ChatClientAgent(chatClient, instructions: prompt, tools: display.All);
```

```razor
<DrylAiStream Source="@_run.TextStream">
  <DrylMarkdown Content="@context.Text" Ai="@context.State" />
</DrylAiStream>
<DrylAgentAttachments Run="@_run" />   @* charts / stats / timeline glide in here *@
```
```

Renumber the following sections (old 4 → 5, old 5 → 6).

- [ ] **Step 4: Build both packages**

Run: `dotnet build DRYL.Components.Agents/DRYL.Components.Agents.csproj`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add CHANGELOG.md DRYL.Components/DRYL.Components.csproj DRYL.Components.Agents/DRYL.Components.Agents.csproj DRYL.Components.Agents/PACKAGE.md
git commit -m "docs: cut 1.3.0 — display tools + DrylPresence Speed; Agents 0.3.0"
```

---

### Task 7: Runtime verification (verify skill)

- [ ] **Step 1:** Launch the docs website (per the `verify` skill), navigate to `/components/agents`.
- [ ] **Step 2:** Drive the new "Display tools" demo with Playwright: click **Ask the analyst**; confirm (a) the assistant bubble streams text, (b) three KPI `DrylStat` cards appear, (c) the line chart and donut chart glide in below, (d) the closing markdown line renders. Screenshot for the user.
- [ ] **Step 3:** Reduced-motion sanity: the presence classes are neutralised by the existing media query (code inspection is sufficient — no new animation primitives were added).
- [ ] **Step 4:** Fix anything found; re-run until green. Final `git log --oneline` sanity check on `feat/display-tools`.
