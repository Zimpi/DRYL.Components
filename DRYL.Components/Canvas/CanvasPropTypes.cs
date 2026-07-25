namespace DRYL.Components.Canvas;

/*  Props of the chart / stat / timeline catalog entries.

    These deliberately mirror — rather than share — the tool-argument specs of
    DRYL.Components.Agents.Tools: those carry [Description] attributes because they
    become a model-facing tool schema, while these are pure canvas props parsed from
    a node's "props" object. After the A1 move the core must not depend on the Agents
    package, and the two now genuinely serve different masters.  */

/// <summary>One series of a <c>lineChart</c> / <c>areaChart</c> / <c>barChart</c> node.</summary>
internal sealed class CanvasChartSeriesProps
{
    /// <summary>Series name shown in the legend and tooltips.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>One value per category label, in label order.</summary>
    public IReadOnlyList<double>? Data { get; set; }
}

/// <summary>Props of the cartesian chart nodes (<c>lineChart</c>, <c>areaChart</c>, <c>barChart</c>).</summary>
internal sealed class CanvasChartProps
{
    /// <summary>Optional caption above the chart.</summary>
    public string? Title { get; set; }

    /// <summary>Category labels along the x axis.</summary>
    public IReadOnlyList<string>? Labels { get; set; }

    /// <summary>The plotted series; each needs exactly one value per label.</summary>
    public IReadOnlyList<CanvasChartSeriesProps>? Series { get; set; }

    /// <summary>.NET numeric format string for tooltip/axis values.</summary>
    public string? ValueFormat { get; set; }

    /// <summary>Bar charts only: stack the series instead of grouping them.</summary>
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

/// <summary>One segment of a <c>donutChart</c> node.</summary>
internal sealed class CanvasChartSegmentProps
{
    /// <summary>Segment name shown in the legend and tooltip.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Segment value; its share of the total drives the angle. Must be greater than 0.</summary>
    public double Value { get; set; }
}

/// <summary>Props of the <c>donutChart</c> node.</summary>
internal sealed class CanvasDonutProps
{
    /// <summary>Optional caption above the chart.</summary>
    public string? Title { get; set; }

    /// <summary>The segments, at most six.</summary>
    public IReadOnlyList<CanvasChartSegmentProps>? Segments { get; set; }

    /// <summary>.NET numeric format string for tooltip values.</summary>
    public string? ValueFormat { get; set; }

    /// <summary>Null when valid; otherwise a corrective, model-facing error sentence.</summary>
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
        return CanvasChartProps.ValidateFormat(ValueFormat);
    }
}

/// <summary>Props of the <c>stat</c> node.</summary>
internal sealed class CanvasStatProps
{
    /// <summary>Short metric label, e.g. 'Revenue'.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>The headline value, pre-formatted as text, e.g. '€184k'.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Optional change indicator text, e.g. '+12.4%'.</summary>
    public string? Delta { get; set; }

    /// <summary>Trend direction of the delta: 'up', 'down' or 'neutral'.</summary>
    public string? Direction { get; set; }

    internal DeltaDirection ParsedDirection => Direction?.ToLowerInvariant() switch
    {
        "up" => DeltaDirection.Up,
        "down" => DeltaDirection.Down,
        "neutral" => DeltaDirection.Neutral,
        _ => DeltaDirection.None,
    };
}

/// <summary>One event of a <c>timeline</c> node.</summary>
internal sealed class CanvasTimelineEventProps
{
    /// <summary>Title line of the event.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional pre-formatted timestamp, e.g. '09:24' or 'May 12'.</summary>
    public string? Timestamp { get; set; }

    /// <summary>Optional body text below the title.</summary>
    public string? Text { get; set; }

    /// <summary>Optional marker tint: 'default', 'success', 'warning' or 'danger'.</summary>
    public string? Kind { get; set; }

    internal TimelineVariant ParsedVariant => Kind?.ToLowerInvariant() switch
    {
        "success" => TimelineVariant.Success,
        "warning" => TimelineVariant.Warning,
        "danger" => TimelineVariant.Danger,
        _ => TimelineVariant.Default,
    };
}

/// <summary>Props of the <c>timeline</c> node.</summary>
internal sealed class CanvasTimelineProps
{
    /// <summary>Optional caption above the timeline.</summary>
    public string? Title { get; set; }

    /// <summary>The events, in display order.</summary>
    public IReadOnlyList<CanvasTimelineEventProps>? Events { get; set; }

    /// <summary>Null when valid; otherwise a corrective, model-facing error sentence.</summary>
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
