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
