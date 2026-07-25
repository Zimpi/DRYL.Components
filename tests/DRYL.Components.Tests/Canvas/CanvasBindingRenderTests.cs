using System.Text.Json;
using Bunit;
using DRYL.Components.Canvas;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>The render side of Canvas Data Binding: what a bound node shows while loading,
/// after a refresh, and when its source goes wrong.</summary>
public class CanvasBindingRenderTests : BunitContext
{
    public sealed record RegionParams(string? Region = null);

    public CanvasBindingRenderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static CanvasSpec Parse(string json) =>
        JsonSerializer.Deserialize<CanvasSpec>(json, CanvasJson.Options)!;

    private void Source(string name, Func<CanvasDataContext, CancellationToken, Task<CanvasScalarData>> handler) =>
        Services.AddDrylCanvasDataSource(name, "Test source.", handler);

    private const string StatSpecJson = """
        {"root":{"id":"root","type":"stack","children":[
            {"id":"s1","type":"stat","props":{"label":"Offen"},
             "data":{"source":"orders.open"}}]}}
        """;

    [Fact]
    public void First_load_shows_a_skeleton_then_the_value()
    {
        var gate = new TaskCompletionSource();
        Services.AddDrylComponents();
        Source("orders.open", async (_, _) => { await gate.Task; return CanvasData.Scalar(12, "12"); });

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse(StatSpecJson)));

        Assert.NotEmpty(cut.FindAll(".skel, .skeleton"));   // loading, not yet broken
        Assert.Empty(cut.FindAll(".stat"));

        gate.SetResult();
        cut.WaitForAssertion(() => Assert.Contains("12", cut.Find(".stat").TextContent),
                             TimeSpan.FromSeconds(3));
        Assert.Empty(cut.FindAll(".skel, .skeleton"));
    }

    [Fact]
    public async Task A_refresh_with_a_new_value_pulses_and_shows_no_skeleton()
    {
        var value = 12d;
        Services.AddDrylComponents();
        Source("orders.open", (_, _) => Task.FromResult(CanvasData.Scalar(value, value.ToString("0"))));

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse(StatSpecJson)));
        cut.WaitForAssertion(() => Assert.Contains("12", cut.Find(".stat").TextContent),
                             TimeSpan.FromSeconds(3));
        Assert.Empty(cut.FindAll(".canvas-pulse"));

        value = 19d;
        await cut.InvokeAsync(() => cut.Instance.Context.Binder!.RefreshAllAsync());

        // The node keeps its identity: the value is set and the pulse carries the movement —
        // exactly what an AI setProps looks like (A8). No skeleton, no rebuild.
        cut.WaitForAssertion(() => Assert.Contains("19", cut.Find(".stat").TextContent),
                             TimeSpan.FromSeconds(3));
        Assert.NotEmpty(cut.FindAll(".canvas-pulse"));
        Assert.Empty(cut.FindAll(".skel, .skeleton"));
    }

    [Fact]
    public async Task A_refresh_without_a_change_pulses_nothing()
    {
        Services.AddDrylComponents();
        Source("orders.open", (_, _) => Task.FromResult(CanvasData.Scalar(12, "12")));

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse(StatSpecJson)));
        cut.WaitForAssertion(() => Assert.Contains("12", cut.Find(".stat").TextContent),
                             TimeSpan.FromSeconds(3));

        await cut.InvokeAsync(() => cut.Instance.Context.Binder!.RefreshAllAsync());
        await Task.Delay(100);

        // Otherwise a 30-second interval blinks the whole dashboard for nothing.
        Assert.Empty(cut.FindAll(".canvas-pulse"));
    }

    [Fact]
    public async Task An_error_after_a_good_value_keeps_the_value_and_marks_it()
    {
        var fail = false;
        Services.AddDrylComponents();
        Source("orders.open", (_, _) => fail
            ? Task.FromException<CanvasScalarData>(new InvalidOperationException("boom"))
            : Task.FromResult(CanvasData.Scalar(12, "12")));

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse(StatSpecJson)));
        cut.WaitForAssertion(() => Assert.Contains("12", cut.Find(".stat").TextContent),
                             TimeSpan.FromSeconds(3));

        fail = true;
        await cut.InvokeAsync(() => cut.Instance.Context.Binder!.RefreshAllAsync());

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".canvas-data-flag")), TimeSpan.FromSeconds(3));
        Assert.Contains("12", cut.Find(".stat").TextContent);  // "briefly disturbed" is not "broken"
        Assert.Empty(cut.FindAll(".canvas-data-error"));
    }

    [Fact]
    public void An_error_without_a_good_value_shows_a_compact_inline_error()
    {
        Services.AddDrylComponents();
        Source("orders.open", (_, _) =>
            Task.FromException<CanvasScalarData>(new InvalidOperationException("boom")));

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"s1","type":"stat","props":{"label":"Offen"},"data":{"source":"orders.open"}},
                {"id":"s2","type":"stat","props":{"label":"Fest","value":"7"}}]}}
            """)));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".canvas-data-error")), TimeSpan.FromSeconds(3));
        Assert.Contains("orders.open", cut.Markup);
        Assert.DoesNotContain("boom", cut.Markup);          // the exception goes to ILogger, not the user
        // A broken widget must never take the dashboard with it.
        Assert.Contains("Fest", cut.Markup);
        Assert.Single(cut.FindAll(".stat"));
    }

    [Fact]
    public void The_refresh_button_appears_only_with_a_binding_and_is_labelled()
    {
        Services.AddDrylComponents();
        Source("orders.open", (_, _) => Task.FromResult(CanvasData.Scalar(1, "1")));

        var unbound = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse("""
            {"root":{"id":"root","type":"stat","props":{"label":"Fest","value":"7"}}}
            """)));
        Assert.DoesNotContain("Refresh data", unbound.Markup);

        var bound = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse(StatSpecJson)));

        var button = bound.Find("button[aria-label='Refresh data']");   // rule 2.9
        Assert.NotNull(button);
        Assert.Contains("Refresh data", bound.Markup);                  // and the tooltip, rule 2.11
    }

    [Fact]
    public async Task A_field_change_reloads_the_dependent_node_only()
    {
        var calls = 0;
        Services.AddDrylComponents();
        Services.AddDrylCanvasDataSource("sales.byRegion", "Umsatz.",
            (RegionParams p, CanvasDataContext _, CancellationToken _) =>
            {
                Interlocked.Increment(ref calls);
                return Task.FromResult(CanvasData.Scalar(p.Region == "north" ? 42 : 7,
                                                         p.Region == "north" ? "42" : "7"));
            });

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"f","type":"select","props":{"name":"region","label":"Region","options":["north","south"]}},
                {"id":"s1","type":"stat","props":{"label":"Umsatz"},
                 "data":{"source":"sales.byRegion","params":{"region":{"$field":"region"}}}}]}}
            """)));

        cut.WaitForAssertion(() => Assert.Equal(1, calls), TimeSpan.FromSeconds(3));

        // One select at the top, and the dependent node follows — without an AI turn (D3).
        await cut.InvokeAsync(() => cut.Instance.Context.Form.Set("region", "north"));

        cut.WaitForAssertion(() => Assert.Equal(2, calls), TimeSpan.FromSeconds(3));
        cut.WaitForAssertion(() => Assert.Contains("42", cut.Find(".stat").TextContent),
                             TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void A_bound_chart_needs_no_labels_in_the_spec()
    {
        // The point of binding: the model stops writing numbers, so the authored props carry
        // presentation only — and that must still validate.
        Services.AddDrylComponents();
        Services.AddDrylCanvasDataSource("sales.byMonth", "Umsatz.",
            (CanvasDataContext _, CancellationToken _) =>
                Task.FromResult(CanvasData.Series(new[] { "Jan", "Feb" }, ("Umsatz", new[] { 1d, 2d }))));

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse("""
            {"root":{"id":"c1","type":"lineChart","props":{"title":"Umsatz je Monat"},
             "data":{"source":"sales.byMonth"}}}
            """)));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("svg")), TimeSpan.FromSeconds(3));
        Assert.Contains("Umsatz je Monat", cut.Markup);
        Assert.Empty(cut.FindAll(".canvas-invalid"));
    }

    [Fact]
    public void A_shape_that_does_not_fit_the_node_type_says_so_at_the_node()
    {
        Services.AddDrylComponents();
        Services.AddDrylCanvasDataSource("orders.rows", "Aufträge.",
            (CanvasDataContext _, CancellationToken _) =>
                Task.FromResult(CanvasData.Rows(new[] { "Nr" }, new[] { new[] { "4711" } })));

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse("""
            {"root":{"id":"c1","type":"lineChart","props":{"title":"Falsch"},
             "data":{"source":"orders.rows"}}}
            """)));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".canvas-invalid")), TimeSpan.FromSeconds(3));
        Assert.Contains("rows", cut.Find(".canvas-invalid").TextContent);
    }

    [Fact]
    public void A_truncated_rows_result_says_so()
    {
        Services.AddDrylComponents();
        Services.AddDrylCanvasDataSource("orders.many", "Viele Aufträge.",
            (CanvasDataContext _, CancellationToken _) => Task.FromResult(CanvasData.Rows(
                new[] { "Nr" }, Enumerable.Range(0, 45).Select(i => new[] { i.ToString() }))));

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse("""
            {"root":{"id":"t1","type":"table","props":{},"data":{"source":"orders.many"}}}
            """)));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".canvas-data-truncated")),
                             TimeSpan.FromSeconds(3));
        Assert.Contains("first 30 rows", cut.Find(".canvas-data-truncated").TextContent);
    }
}
