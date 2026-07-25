namespace DRYL.Components.Canvas;

/// <summary>The four result shapes a canvas data source can return.</summary>
public enum CanvasDataShape
{
    /// <summary>One number and/or one piece of text — <c>stat</c>, <c>badge</c>, <c>progress</c>.</summary>
    Scalar,

    /// <summary>Category labels plus one or more named number series — the cartesian charts.</summary>
    Series,

    /// <summary>Labelled parts of a whole — <c>donutChart</c>.</summary>
    Segments,

    /// <summary>Columns plus string rows — <c>table</c>.</summary>
    Rows,
}

/// <summary>
/// The result of a canvas data source. A host thinks in data, not in widgets: it returns one of the
/// four shapes and the binder maps it onto whichever node type the artifact happens to use.
/// Create instances through the factory methods — the shapes themselves have no public constructors
/// so a source can never hand back a half-built result.
/// </summary>
public abstract class CanvasData
{
    private protected CanvasData() { }

    /// <summary>Which of the four shapes this result is.</summary>
    public abstract CanvasDataShape Shape { get; }

    /// <summary>A numeric scalar, optionally with pre-formatted display text and a delta.</summary>
    /// <param name="value">The number. <c>stat</c> tweens it, <c>progress</c> expects 0–100.</param>
    /// <param name="text">Pre-formatted display text; defaults to the invariant number.</param>
    /// <param name="delta">Optional change indicator, e.g. <c>"+12.4%"</c>.</param>
    /// <param name="direction">Trend of the delta: <c>up</c>, <c>down</c> or <c>neutral</c>.</param>
    public static CanvasScalarData Scalar(double value, string? text = null,
                                          string? delta = null, string? direction = null) =>
        new(value, text ?? FormattableString.Invariant($"{value}"), delta, direction);

    /// <summary>A textual scalar — valid on <c>stat</c> and <c>badge</c>, but not on <c>progress</c>.</summary>
    public static CanvasScalarData Scalar(string text, string? delta = null, string? direction = null) =>
        new(null, text, delta, direction);

    /// <summary>Category labels plus one or more named series; every series needs one value per label.</summary>
    public static CanvasSeriesData Series(IEnumerable<string> labels,
                                          params (string Name, IEnumerable<double> Data)[] series) =>
        new(labels?.ToList() ?? new List<string>(),
            (series ?? Array.Empty<(string, IEnumerable<double>)>())
                .Select(s => new CanvasDataSeries(s.Name, s.Data?.ToList() ?? new List<double>()))
                .ToList());

    /// <summary>Labelled parts of a whole.</summary>
    public static CanvasSegmentData Segments(IEnumerable<(string Label, double Value)> segments) =>
        new((segments ?? Array.Empty<(string, double)>())
            .Select(s => new CanvasDataSegment(s.Label, s.Value)).ToList());

    /// <summary>Column headers plus string rows; each row must have one cell per column.</summary>
    public static CanvasRowData Rows(IEnumerable<string> columns, IEnumerable<IEnumerable<string>> rows) =>
        new(columns?.ToList() ?? new List<string>(),
            (rows ?? Array.Empty<IEnumerable<string>>())
                .Select(r => (IReadOnlyList<string>)(r?.ToList() ?? new List<string>())).ToList());
}

/// <summary>One number and/or one piece of text (<see cref="CanvasDataShape.Scalar"/>).</summary>
public sealed class CanvasScalarData : CanvasData
{
    internal CanvasScalarData(double? value, string text, string? delta, string? direction)
    {
        Value = value;
        Text = text;
        Delta = delta;
        Direction = direction;
    }

    /// <inheritdoc />
    public override CanvasDataShape Shape => CanvasDataShape.Scalar;

    /// <summary>The number, or <c>null</c> for a text-only scalar.</summary>
    public double? Value { get; }

    /// <summary>Pre-formatted display text — always present.</summary>
    public string Text { get; }

    /// <summary>Optional change indicator, e.g. <c>"+12.4%"</c>.</summary>
    public string? Delta { get; }

    /// <summary>Trend of the delta: <c>up</c>, <c>down</c> or <c>neutral</c>.</summary>
    public string? Direction { get; }
}

/// <summary>One named number series.</summary>
public sealed record CanvasDataSeries(string Name, IReadOnlyList<double> Data);

/// <summary>Category labels plus named series (<see cref="CanvasDataShape.Series"/>).</summary>
public sealed class CanvasSeriesData : CanvasData
{
    internal CanvasSeriesData(IReadOnlyList<string> labels, IReadOnlyList<CanvasDataSeries> series)
    {
        Labels = labels;
        Series = series;
    }

    /// <inheritdoc />
    public override CanvasDataShape Shape => CanvasDataShape.Series;

    /// <summary>The category labels along the x axis.</summary>
    public IReadOnlyList<string> Labels { get; }

    /// <summary>The plotted series.</summary>
    public IReadOnlyList<CanvasDataSeries> Series { get; }
}

/// <summary>One labelled part of a whole.</summary>
public sealed record CanvasDataSegment(string Label, double Value);

/// <summary>Labelled parts of a whole (<see cref="CanvasDataShape.Segments"/>).</summary>
public sealed class CanvasSegmentData : CanvasData
{
    internal CanvasSegmentData(IReadOnlyList<CanvasDataSegment> segments) => Segments = segments;

    /// <inheritdoc />
    public override CanvasDataShape Shape => CanvasDataShape.Segments;

    /// <summary>The segments.</summary>
    public IReadOnlyList<CanvasDataSegment> Segments { get; }
}

/// <summary>Columns plus string rows (<see cref="CanvasDataShape.Rows"/>).</summary>
public sealed class CanvasRowData : CanvasData
{
    internal CanvasRowData(IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        Columns = columns;
        Rows = rows;
    }

    /// <inheritdoc />
    public override CanvasDataShape Shape => CanvasDataShape.Rows;

    /// <summary>The column headers.</summary>
    public IReadOnlyList<string> Columns { get; }

    /// <summary>The rows; each has one cell per column.</summary>
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; }
}
