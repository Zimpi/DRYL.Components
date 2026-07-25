using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>
/// The model contract for data binding, driven through the real tools with a replayed
/// generation. Every mistake must come back as one corrective sentence in the receipt —
/// and the artifact must render anyway (the invalid node becomes a placeholder).
/// </summary>
public class CanvasDataReceiptTests
{
    public sealed record SalesParams(int Year, string? Region = null);

    private static ICanvasDataService Data()
    {
        var services = new ServiceCollection();
        services.AddDrylComponents();
        services.AddDrylCanvasDataSource("sales.byMonth", "Umsatz je Monat in Tsd €.",
            (SalesParams _, CanvasDataContext _, CancellationToken _) =>
                Task.FromResult(CanvasData.Series(new[] { "Jan" }, ("Umsatz", new[] { 1d }))));
        services.AddDrylCanvasDataSource("orders.open", "Offene Aufträge.",
            (CanvasDataContext _, CancellationToken _) =>
                Task.FromResult(CanvasData.Rows(new[] { "Nr" }, new[] { new[] { "4711" } })));
        return services.BuildServiceProvider().CreateScope().ServiceProvider
            .GetRequiredService<ICanvasDataService>();
    }

    private static async IAsyncEnumerable<string> Script(string json)
    {
        await Task.Yield();
        yield return json;
    }

    /// <summary>Runs create_artifact against a replayed generation; returns the receipt and,
    /// out of band, the prompt the generator was handed.</summary>
    private static async Task<(string Receipt, string Prompt)> CreateAsync(DrylCanvasRun run, string specJson)
    {
        string? prompt = null;
        var tools = DrylCanvasTools.CreateReplay(run, (p, _) => { prompt = p; return Script(specJson); }, Data());
        var result = await ((AIFunction)tools.CreateArtifact)
            .InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?> { ["brief"] = "b" }!));
        return (result?.ToString() ?? string.Empty, prompt ?? string.Empty);
    }

    [Fact]
    public async Task The_generation_prompt_carries_the_source_catalog()
    {
        var run = new DrylCanvasRun();
        var (_, prompt) = await CreateAsync(run, """
            {"title":"T","root":{"id":"c","type":"lineChart",
             "data":{"source":"sales.byMonth","params":{"year":2026}}}}
            """);

        Assert.Contains("DATA SOURCES", prompt);
        Assert.Contains("sales.byMonth(year: int, region?: string) -> series", prompt);
        Assert.Contains("Do NOT invent numbers", prompt);
    }

    [Fact]
    public async Task A_valid_binding_produces_a_clean_receipt()
    {
        var run = new DrylCanvasRun();

        var (receipt, _) = await CreateAsync(run, """
            {"title":"T","root":{"id":"c","type":"lineChart","props":{"title":"Umsatz"},
             "data":{"source":"sales.byMonth","params":{"year":2026},"refresh":"interval:30s"}}}
            """);

        Assert.Contains("Artifact created", receipt);
        Assert.DoesNotContain("invalid", receipt);
        Assert.NotNull(run.Spec?.Root);
    }

    [Theory]
    [InlineData("""{"source":"sales.byWeek","params":{"year":2026}}""", "unknown data source 'sales.byWeek'")]
    [InlineData("""{"source":"orders.open"}""", "returns rows")]
    [InlineData("""{"source":"sales.byMonth"}""", "missing required param year")]
    [InlineData("""{"source":"sales.byMonth","params":{"year":2026,"region":{"$field":"gebiet"}}}""",
                "references field 'gebiet'")]
    [InlineData("""{"source":"sales.byMonth","params":{"year":2026},"refresh":"interval:1s"}""",
                "below the 5s floor")]
    public async Task Every_binding_mistake_comes_back_as_a_corrective_sentence(string data, string expected)
    {
        var run = new DrylCanvasRun();

        var (receipt, _) = await CreateAsync(run,
            """{"title":"T","root":{"id":"c","type":"lineChart","props":{"title":"Umsatz"},"data":"""
            + data + "}}");

        Assert.Contains(expected, receipt);
        Assert.Contains("fix via update_artifact", receipt);
        // No hard stop: the artifact is there, the broken node renders as a placeholder.
        Assert.NotNull(run.Spec?.Root);
        Assert.Equal(AiState.Generated, run.State);
    }

    [Fact]
    public async Task A_field_reference_to_a_node_of_the_same_artifact_is_accepted()
    {
        var run = new DrylCanvasRun();

        var (receipt, _) = await CreateAsync(run, """
            {"title":"T","root":{"id":"root","type":"stack","children":[
                {"id":"f","type":"select","props":{"name":"region","label":"Region","options":["north","south"]}},
                {"id":"c","type":"lineChart","props":{"title":"Umsatz"},
                 "data":{"source":"sales.byMonth","params":{"year":2026,"region":{"$field":"region"}}}}]}}
            """);

        Assert.DoesNotContain("invalid", receipt);
    }

    [Fact]
    public async Task Without_registered_sources_nothing_about_data_reaches_the_model()
    {
        // A2: an app that registers no sources must see the exact prompt and receipts it saw before.
        var run = new DrylCanvasRun();
        string? prompt = null;
        var tools = DrylCanvasTools.CreateReplay(run, (p, _) =>
        {
            prompt = p;
            return Script("""{"title":"T","root":{"id":"s","type":"stat","props":{"label":"L","value":"1"}}}""");
        });

        var receipt = await ((AIFunction)tools.CreateArtifact)
            .InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?> { ["brief"] = "b" }!));

        Assert.DoesNotContain("DATA SOURCES", prompt);
        Assert.DoesNotContain("invalid", receipt?.ToString());
    }
}
