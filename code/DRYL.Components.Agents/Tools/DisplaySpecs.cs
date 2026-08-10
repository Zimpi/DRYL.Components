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
            if (s.Direction is not (null or "up" or "down" or "neutral"))
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
            if (e.Kind is not (null or "default" or "success" or "warning" or "danger"))
                return $"kind '{e.Kind}' is invalid — use 'default', 'success', 'warning' or 'danger'.";
        }
        return null;
    }
}
