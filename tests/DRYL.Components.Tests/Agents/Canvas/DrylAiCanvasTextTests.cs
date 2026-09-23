using System.Text.Json;
using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>
/// Every text the AI canvas shows or announces is a parameter, with the previous English
/// wording as its default — so a German host's status pill and screen reader speak German.
/// </summary>
public class DrylAiCanvasTextTests : BunitContext
{
    public DrylAiCanvasTextTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static CanvasSpec Parse(string json) =>
        JsonSerializer.Deserialize<CanvasSpec>(json, CanvasJson.Options)!;

    private static CanvasSpec Untitled() => Parse("""
        {"root":{"id":"root","type":"stack","children":[
            {"id":"a","type":"markdown","props":{"content":"x"}}]}}
        """);

    private static string Pill(IRenderedComponent<DrylAiCanvas> cut) => cut.Find(".ai-indicator").TextContent.Trim();

    private static string Live(IRenderedComponent<DrylAiCanvas> cut) => cut.FindAll(".canvas-live")[0].TextContent.Trim();

    [Fact]
    public void The_defaults_are_unchanged()
    {
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, new DrylCanvasRun()));

        Assert.Contains("No artifact yet", cut.Markup);
        Assert.Contains("Ask the assistant to create one.", cut.Markup);
        Assert.Equal("Artifact", cut.Find(".canvas-title").TextContent);
        Assert.Equal("Idle", Pill(cut));
    }

    [Fact]
    public void The_empty_state_and_the_fallback_title_can_be_overridden()
    {
        var cut = Render<DrylAiCanvas>(p => p
            .Add(x => x.Run, new DrylCanvasRun())
            .Add(x => x.EmptyTitle, "Noch nichts gezeichnet")
            .Add(x => x.EmptyText, "Bitte Santiago, etwas zu zeigen.")
            .Add(x => x.FallbackTitle, "Leinwand")
            .Add(x => x.IdleText, "Bereit"));

        Assert.Contains("Noch nichts gezeichnet", cut.Markup);
        Assert.Contains("Bitte Santiago, etwas zu zeigen.", cut.Markup);
        Assert.Equal("Leinwand", cut.Find(".canvas-title").TextContent);
        Assert.Equal("Bereit", Pill(cut));
    }

    [Fact]
    public void Building_speaks_the_hosts_language()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        run.RevealSnapshot(Untitled());

        var cut = Render<DrylAiCanvas>(p => p
            .Add(x => x.Run, run)
            .Add(x => x.BuildingText, n => $"Baut · {n}")
            .Add(x => x.BuildingAnnouncement, "Leinwand wird gebaut…"));

        Assert.Matches(@"^Baut · \d+$", Pill(cut));
        Assert.Equal("Leinwand wird gebaut…", Live(cut));
    }

    [Fact]
    public void Default_building_line_is_unchanged()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        run.RevealSnapshot(Untitled());

        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));

        Assert.StartsWith("Building", Pill(cut));
        Assert.Equal("Building artifact…", Live(cut));
    }

    [Fact]
    public void Ready_speaks_the_hosts_language()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        run.CompleteGeneration(Untitled());

        var cut = Render<DrylAiCanvas>(p => p
            .Add(x => x.Run, run)
            .Add(x => x.ReadyText, n => $"Fertig · {n}")
            .Add(x => x.ReadyAnnouncement, "Leinwand fertig."));

        Assert.Matches(@"^Fertig · \d+$", Pill(cut));
        Assert.Equal("Leinwand fertig.", Live(cut));
    }

    [Fact]
    public void A_failure_speaks_the_hosts_language()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        run.FailGeneration(new InvalidOperationException("boom"));

        var cut = Render<DrylAiCanvas>(p => p
            .Add(x => x.Run, run)
            .Add(x => x.FailedAnnouncement, "Leinwand fehlgeschlagen.")
            .Add(x => x.ErrorTitle, "Das hat nicht geklappt"));

        Assert.Equal("Leinwand fehlgeschlagen.", Live(cut));
        Assert.Contains("Das hat nicht geklappt", cut.Markup);
    }

    [Fact]
    public void An_update_announcement_speaks_the_hosts_language()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        run.CompleteGeneration(Untitled());
        run.BeginGeneration();
        run.CompleteGeneration();

        var cut = Render<DrylAiCanvas>(p => p
            .Add(x => x.Run, run)
            .Add(x => x.UpdatedAnnouncement, n => $"Aktualisiert, {n} Änderungen."));

        Assert.Matches(@"^Aktualisiert, \d+ Änderungen\.$", Live(cut));
    }

    [Fact]
    public void The_core_canvas_texts_can_be_overridden()
    {
        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.EmptyTitle, "Leer")
            .Add(x => x.FallbackTitle, "Ohne Titel"));

        Assert.Contains("Leer", cut.Markup);
        Assert.Equal("Ohne Titel", cut.Find(".canvas-title").TextContent);

        cut.Render(p => p.Add(x => x.Error, "kaputt").Add(x => x.ErrorTitle, "Fehler"));
        Assert.Contains("Fehler", cut.Find(".alert").TextContent);
    }
}
