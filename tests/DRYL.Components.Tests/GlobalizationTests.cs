using System.Globalization;
using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Culture-safety regression tests. DRYL interpolates numbers into CSS and SVG
/// in several components; under a comma-decimal locale (e.g. German) an
/// unguarded <c>0.5</c> becomes <c>0,5</c>, which silently breaks CSS lengths
/// and SVG point parsers. Every numeric→markup interpolation must use
/// <see cref="CultureInfo.InvariantCulture"/>; these tests render under
/// <c>de-DE</c> and assert the emitted numbers stay dot-decimal.
/// </summary>
public class GlobalizationTests : BunitContext
{
    private static void UnderCulture(string name, Action body)
    {
        var prevCulture = CultureInfo.CurrentCulture;
        var prevUi = CultureInfo.CurrentUICulture;
        try
        {
            var culture = CultureInfo.GetCultureInfo(name);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            body();
        }
        finally
        {
            CultureInfo.CurrentCulture = prevCulture;
            CultureInfo.CurrentUICulture = prevUi;
        }
    }

    [Fact]
    public void Progress_fill_width_is_dot_decimal_under_german_culture()
    {
        UnderCulture("de-DE", () =>
        {
            // 1 / 3 → 33.33%, a value that needs a decimal separator.
            var cut = Render<DrylProgress>(ps => ps
                .Add(p => p.Value, 1d)
                .Add(p => p.Max, 3d));

            Assert.Contains("33.33%", cut.Markup);
            Assert.DoesNotContain("33,33", cut.Markup);
        });
    }

    [Fact]
    public void Slider_percent_is_dot_decimal_under_german_culture()
    {
        UnderCulture("de-DE", () =>
        {
            // Value 1 on a 0..8 range → 12.50%.
            var cut = Render<DrylSlider>(ps => ps
                .Add(p => p.Min, 0d)
                .Add(p => p.Max, 8d)
                .Add(p => p.Step, 1d)
                .Add(p => p.Value, 1d));

            // The --pct custom property drives the fill; it must never contain a comma.
            var input = cut.Find("input[type=range]");
            var style = input.GetAttribute("style") ?? "";
            Assert.Contains("--pct:", style);
            Assert.DoesNotContain(",", style);
        });
    }
}
