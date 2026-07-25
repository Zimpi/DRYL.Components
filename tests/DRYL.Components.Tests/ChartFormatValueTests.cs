using DRYL.Components;
using Xunit;

namespace DRYL.Components.Tests;

/// <summary>
/// <c>FormatValue</c> template support: <c>{value}</c> placeholders (what AI models
/// naturally emit), optional inner .NET format, back-compat with plain .NET format
/// strings, malformed-template fallback. Assertions mirror the culture via
/// <c>v.ToString(...)</c> instead of hardcoding separators.
/// </summary>
public class ChartFormatValueTests
{
    private sealed class TestChart : DrylChartBase
    {
        public string Format(double v) => FormatValue(v);
    }

    [Fact]
    public void Null_format_uses_default() =>
        Assert.Equal(80.ToString("0.##"), new TestChart().Format(80));

    [Fact]
    public void Dotnet_format_string_still_works() =>
        Assert.Equal(80.ToString("N0"), new TestChart { ValueFormat = "N0" }.Format(80));

    [Fact]
    public void Value_template_substitutes_the_number()
    {
        var chart = new TestChart { ValueFormat = "€{value} Tsd" };
        Assert.Equal($"€{80.ToString("0.##")} Tsd", chart.Format(80));
    }

    [Fact]
    public void Percent_template_substitutes_without_duplication() =>
        Assert.Equal("17%", new TestChart { ValueFormat = "{value}%" }.Format(17));

    [Fact]
    public void Inner_format_controls_the_number()
    {
        var chart = new TestChart { ValueFormat = "{value:0.0}%" };
        Assert.Equal(17.5.ToString("0.0") + "%", chart.Format(17.5));
    }

    [Fact]
    public void Malformed_template_falls_back_to_dotnet_formatting()
    {
        // "{valueX}" is not our placeholder — treated as a plain .NET format string.
        var chart = new TestChart { ValueFormat = "{valueX}" };
        Assert.Equal(80.ToString("{valueX}"), chart.Format(80));
    }

    [Fact]
    public void Invalid_inner_format_falls_back_to_default()
    {
        // "{value:Q}" — a plausible model emission (K=Tsd., M=Mio.) — is not a valid
        // .NET format string; it must not throw mid chart-render.
        var chart = new TestChart { ValueFormat = "{value:Q}" };
        Assert.Equal(80.ToString("0.##"), chart.Format(80));
    }

    [Fact]
    public void Invalid_plain_format_falls_back_to_default()
    {
        var chart = new TestChart { ValueFormat = "K" };
        Assert.Equal(80.ToString("0.##"), chart.Format(80));
    }
}
