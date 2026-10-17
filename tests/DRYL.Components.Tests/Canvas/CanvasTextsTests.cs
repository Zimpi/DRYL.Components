using System.Text.Json;
using Bunit;
using DRYL.Components.Agents;
using DRYL.Components.Ai;
using DRYL.Components.Canvas;
using DRYL.Components.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>
/// Every fixed word of the canvas's own chrome comes from <see cref="CanvasTexts"/>, with the
/// previous English wording as its default — so a German host's tooltips, accessible names and
/// screen-reader announcements speak German.
/// </summary>
public class CanvasTextsTests : BunitContext
{
    public CanvasTextsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static CanvasSpec Parse(string json) =>
        JsonSerializer.Deserialize<CanvasSpec>(json, CanvasJson.Options)!;

    private static CanvasSpec TwoStats() => Parse("""
        { "root": { "id": "root", "type": "stack", "children": [
            { "id": "a", "type": "stat", "props": { "label": "Revenue", "value": "1" } },
            { "id": "b", "type": "stat", "props": { "label": "Orders", "value": "2" } } ] } }
        """);

    private static readonly CanvasTexts German = CanvasTexts.Default with
    {
        Expand = "Vergrößern",
        ExitFullscreen = "Vollbild verlassen",
        Refresh = "Daten aktualisieren",
        Prompt = "Zu diesem Element fragen",
        Pin = "Element anheften",
        Unpin = "Element lösen",
        Duplicate = "Element duplizieren",
        Remove = "Element entfernen",
        Reorder = "Element verschieben",
        PinnedState = "angeheftet",
        TypeName = type => type == "stat" ? "Kennzahl" : CanvasTexts.Default.TypeName(type),
        WaitingFor = type => $"warte auf {type} …",
        Selected = (label, type) => $"Ausgewählt: {label} ({type})",
        SelectionCleared = "Auswahl aufgehoben.",
        Pinned = label => $"{label} angeheftet.",
        Duplicated = label => $"{label} dupliziert.",
        MovedTo = (label, position, count) => $"{label} an Position {position} von {count}.",
        PinEdit = label => $"{label} angeheftet",
    };

    private IRenderedComponent<DrylCanvas> Canvas(CanvasSelection sel, List<CanvasEdit>? edits = null) =>
        Render<DrylCanvas>(p => p
            .Add(x => x.Spec, TwoStats())
            .Add(x => x.Selection, sel)
            .Add(x => x.Texts, German)
            .Add(x => x.OnEdit, e => edits?.Add(e)));

    private static string Live(IRenderedComponent<DrylCanvas> cut) =>
        cut.FindAll(".canvas-live")[^1].TextContent.Trim();

    [Fact]
    public void The_defaults_are_the_previous_english_texts()
    {
        var texts = CanvasTexts.Default;

        Assert.Equal("Expand artifact", texts.Expand);
        Assert.Equal("Exit fullscreen", texts.ExitFullscreen);
        Assert.Equal("Refresh data", texts.Refresh);
        Assert.Equal("Selected: Revenue, stat", texts.Selected("Revenue", "stat"));
        Assert.Equal("Selection cleared.", texts.SelectionCleared);
        Assert.Equal("Revenue moved to position 2 of 3.", texts.MovedTo("Revenue", 2, 3));
        Assert.Equal("waiting for stat…", texts.WaitingFor("stat"));
        Assert.Equal("Showing the first 30 rows.", texts.Truncated(30));
        Assert.Equal("Pinned Revenue", texts.PinEdit("Revenue"));
        Assert.Equal("Source 'orders.open' is unavailable.", texts.SourceUnavailable("orders.open"));
        Assert.Equal("Action 'order.approve' failed.", texts.ActionFailed("order.approve"));
        Assert.Equal("Confirmation is unavailable — the action was not run.", texts.ConfirmUnavailable);
        Assert.Equal("Confirm", texts.Confirm);
        Assert.Equal("Line chart", texts.TypeName("lineChart"));
        Assert.Equal("Element", texts.TypeName(null));
        Assert.Equal("Undo", texts.Undo);
        Assert.Equal("Redo", texts.Redo);
        Assert.Equal("Version history", texts.VersionHistory);
        Assert.Equal("Undone: Pinned Revenue", texts.Undone("Pinned Revenue"));
        Assert.Equal("Undone: last change", texts.Undone(null));
        Assert.Equal("Redone: last change", texts.Redone(null));
        Assert.Equal("Restored version 2 of 3", texts.Restored(2, 3));
        Assert.Equal("Close Overview", texts.CloseView("Overview"));
        Assert.Equal("Version 4", texts.VersionLabel(4));
    }

    [Fact]
    public void The_expand_button_speaks_the_hosts_language_in_both_states()
    {
        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, TwoStats()).Add(x => x.Texts, German));

        var button = cut.Find("button[aria-label='Vergrößern']");
        Assert.Contains("Vergrößern", cut.Markup);   // the tooltip too

        button.Click();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("button[aria-label='Vollbild verlassen']")));
        Assert.DoesNotContain("Expand artifact", cut.Markup);
        Assert.DoesNotContain("Exit fullscreen", cut.Markup);
    }

    [Fact]
    public void The_refresh_button_speaks_the_hosts_language()
    {
        Services.AddDrylCanvasDataSource("orders.open", "Test source.",
            (CanvasDataContext _, CancellationToken _) => Task.FromResult(CanvasData.Scalar(1, "1")));

        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Parse("""
                {"root":{"id":"root","type":"stack","children":[
                    {"id":"s1","type":"stat","props":{"label":"Offen"},"data":{"source":"orders.open"}}]}}
                """))
            .Add(x => x.Texts, German));

        Assert.NotNull(cut.Find("button[aria-label='Daten aktualisieren']"));
        Assert.DoesNotContain("Refresh data", cut.Markup);
    }

    [Fact]
    public void The_node_toolbar_speaks_the_hosts_language()
    {
        var cut = Canvas(new CanvasSelection());
        cut.Find("[data-cid='a']").Click();

        foreach (var label in new[]
                 {
                     "Zu diesem Element fragen", "Element anheften", "Element duplizieren",
                     "Element entfernen", "Element verschieben",
                 })
        {
            Assert.NotNull(cut.Find($".canvas-node-tools button[aria-label='{label}']"));
        }
        Assert.DoesNotContain("Prompt about this element", cut.Markup);   // nor in a tooltip
        Assert.DoesNotContain("Duplicate element", cut.Markup);
    }

    [Fact]
    public void Selection_announcements_speak_the_hosts_language()
    {
        var sel = new CanvasSelection();
        var cut = Canvas(sel);

        cut.Find("[data-cid='a']").Click();
        cut.WaitForAssertion(() => Assert.Equal("Ausgewählt: Revenue (Kennzahl)", Live(cut)));

        cut.InvokeAsync(sel.Clear);
        cut.WaitForAssertion(() => Assert.Equal("Auswahl aufgehoben.", Live(cut)));
    }

    [Fact]
    public void Command_announcements_and_edit_labels_speak_the_hosts_language()
    {
        var edits = new List<CanvasEdit>();
        var cut = Canvas(new CanvasSelection(), edits);
        cut.Find("[data-cid='a']").Click();

        cut.Find(".canvas-node-tools button[aria-label='Element anheften']").Click();

        cut.WaitForAssertion(() => Assert.Equal("Revenue angeheftet.", Live(cut)));
        Assert.Equal("Revenue angeheftet", edits[^1].Label);
        // The pinned element's accessible name and the pin button follow the state.
        Assert.Equal("Revenue, Kennzahl, angeheftet", cut.Find("[data-cid='a']").GetAttribute("aria-label"));
        Assert.NotNull(cut.Find(".canvas-node-tools button[aria-label='Element lösen']"));
    }

    [Fact]
    public void A_reorder_announces_the_position_in_the_hosts_language()
    {
        var cut = Canvas(new CanvasSelection());
        cut.Find("[data-cid='a']").Click();

        cut.InvokeAsync(() => cut.Instance.OnNodeReorder("a", 1));

        cut.WaitForAssertion(() => Assert.Equal("Revenue an Position 2 von 2.", Live(cut)));
    }

    [Fact]
    public void The_waiting_caption_speaks_the_hosts_language()
    {
        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Parse("""
                {"root":{"id":"root","type":"stack","children":[
                    {"id":"bad","type":"stat","props":{"label":"only a label"}}]}}
                """))
            .Add(x => x.Ai, AiState.Streaming)
            .Add(x => x.Texts, German));

        Assert.Equal("warte auf Kennzahl …", cut.Find(".canvas-waiting").TextContent);
    }

    private const string SpecWithAction = """
        {"root":{"id":"r","type":"stack","children":[
          {"id":"btn","type":"button","props":{"label":"Freigeben"},
           "action":{"name":"order.approve","args":{"orderId":"4711"}}}]}}
        """;

    [Fact]
    public void A_failing_source_speaks_the_hosts_language()
    {
        Services.AddDrylCanvasDataSource("orders.open", "Test source.",
            (CanvasDataContext _, CancellationToken _) => Task.FromException<CanvasScalarData>(new InvalidOperationException("boom")));

        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Parse("""
                {"root":{"id":"root","type":"stack","children":[
                    {"id":"s1","type":"stat","props":{"label":"Offen"},"data":{"source":"orders.open"}}]}}
                """))
            .Add(x => x.Texts, CanvasTexts.Default with { SourceUnavailable = s => $"Quelle {s} nicht erreichbar." }));

        cut.WaitForAssertion(() => Assert.Contains("Quelle orders.open nicht erreichbar.", cut.Find(".canvas-data-error").TextContent),
                             TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void A_throwing_action_speaks_the_hosts_language()
    {
        Services.AddDrylCanvasAction("order.approve", "Gibt einen Auftrag frei.",
            (ApproveArgs _, CanvasActionContext _, CancellationToken _) =>
                Task.FromException<CanvasActionResult>(new InvalidOperationException("boom")));

        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Parse(SpecWithAction))
            .Add(x => x.Texts, CanvasTexts.Default with { ActionFailed = a => $"Aktion {a} fehlgeschlagen." }));
        cut.Find(".canvas-body button.btn").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Aktion order.approve fehlgeschlagen.", cut.Find(".canvas-action-error").TextContent));
    }

    private static CanvasActionRunner Runner(ServiceProvider sp) =>
        new(sp.GetRequiredService<ICanvasActionService>(), null, new CanvasFormState(), sp)
        {
            Texts = CanvasTexts.Default with
            {
                ConfirmUnavailable = "Keine Bestätigung möglich.",
                Confirm = "Bestätigen",
                Cancel = "Abbrechen",
            },
        };

    private static ServiceCollection ActionServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDrylCanvasAction("a", "…",
            (CanvasActionContext _, CancellationToken _) => Task.FromResult(CanvasActionResult.Ok()));
        return services;
    }

    [Fact]
    public async Task A_missing_confirmation_speaks_the_hosts_language()
    {
        // No IDrylDialogService on purpose: a gated action must not run unconfirmed.
        using var sp = ActionServices().BuildServiceProvider();
        var runner = Runner(sp);

        await runner.InvokeAsync("btn", "Freigeben", new CanvasActionBinding { Name = "a", Confirm = "Wirklich?" });

        Assert.Equal("Keine Bestätigung möglich.", runner.StateOf("btn")!.Error);
    }

    [Fact]
    public async Task The_confirmation_dialog_speaks_the_hosts_language()
    {
        var services = ActionServices();
        services.AddScoped<IDrylDialogService, StubDialogService>();
        using var sp = services.BuildServiceProvider();
        var runner = Runner(sp);

        await runner.InvokeAsync("btn", null, new CanvasActionBinding { Name = "a", Confirm = "Wirklich?" });

        var dialogs = (StubDialogService)sp.GetRequiredService<IDrylDialogService>();
        Assert.Equal("Bestätigen", dialogs.Asked.Single().Title);   // an unlabelled button
        Assert.Equal(("Bestätigen", "Abbrechen"), dialogs.Labels.Single());
    }

    [Fact]
    public void The_ai_canvas_forwards_its_texts()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        run.CompleteGeneration(TwoStats());

        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run).Add(x => x.Texts, German));

        Assert.NotNull(cut.Find("button[aria-label='Vergrößern']"));
        Assert.DoesNotContain("Expand artifact", cut.Markup);
    }
    // ───── the type name and the workspace (I24) ─────

    private static CanvasSpec Unnamed() => Parse("""
        { "root": { "id": "root", "type": "stack", "children": [
            { "id": "c", "type": "lineChart", "props": { "series": [] } },
            { "id": "d", "type": "divider" } ] } }
        """);

    private static readonly CanvasTexts GermanTypes = CanvasTexts.Default with
    {
        TypeName = type => type switch
        {
            "lineChart" => "Liniendiagramm",
            "divider" => "Trennlinie",
            _ => CanvasTexts.Default.TypeName(type),
        },
        Selected = (label, type) => $"Ausgewählt: {label}, {type}",
        RemoveEdit = label => $"{label} entfernt",
    };

    [Fact]
    public void An_unnamed_element_is_named_by_the_hosts_type_name_everywhere()
    {
        var sel = new CanvasSelection();
        var edits = new List<CanvasEdit>();
        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Unnamed())
            .Add(x => x.Selection, sel)
            .Add(x => x.Texts, GermanTypes)
            .Add(x => x.OnEdit, e => edits.Add(e)));

        Assert.Equal("Trennlinie, Trennlinie", cut.Find("[data-cid='d']").GetAttribute("aria-label"));

        cut.Find("[data-cid='d']").Click();
        cut.WaitForAssertion(() => Assert.Equal("Ausgewählt: Trennlinie, Trennlinie", Live(cut)));
        // The shared selection carries the translated name to the dock's chip and the prompt,
        // while the catalog id stays available to the model.
        Assert.Equal("Trennlinie", sel.Label);
        Assert.Equal("divider", sel.Type);

        cut.Find(".canvas-node-tools button[aria-label='Remove element']").Click();
        cut.WaitForAssertion(() => Assert.Equal("Trennlinie entfernt", edits[^1].Label));
    }

    [Fact]
    public void The_raw_type_id_never_reaches_the_accessible_name()
    {
        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Unnamed())
            .Add(x => x.Selection, new CanvasSelection()));

        Assert.Equal("Divider, Divider", cut.Find("[data-cid='d']").GetAttribute("aria-label"));
        Assert.EndsWith(", Line chart", cut.Find("[data-cid='c']").GetAttribute("aria-label"));
    }

    [Fact]
    public void A_blank_host_type_name_falls_back_to_the_default()
    {
        var node = new CanvasNode { Id = "d", Type = "keyValue" };

        Assert.Equal("Key value", CanvasLabel.For(node, _ => " "));
        Assert.Equal("Schlüssel-Wert", CanvasLabel.For(node, _ => "Schlüssel-Wert"));
        Assert.Equal("Key value", CanvasLabel.For(node));   // the old overload is unchanged
    }

    [Fact]
    public void The_workspace_speaks_the_hosts_language()
    {
        var workspace = new CanvasWorkspace();
        workspace.Open("Übersicht").Spec = TwoStats();

        var cut = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, workspace)
            .Add(x => x.ShowHistory, true)
            .Add(x => x.Texts, CanvasTexts.Default with
            {
                Undo = "Rückgängig",
                Redo = "Wiederholen",
                VersionHistory = "Versionsverlauf",
                CloseView = title => $"{title} schließen",
                Expand = "Vergrößern",
            }));

        Assert.NotNull(cut.Find(".ws-tools button[aria-label='Rückgängig']"));
        Assert.NotNull(cut.Find(".ws-tools button[aria-label='Wiederholen']"));
        Assert.NotNull(cut.Find(".ws-tools button[aria-label='Versionsverlauf']"));
        Assert.NotNull(cut.Find("button[aria-label='Übersicht schließen']"));
        // The fallback body canvas receives the same texts.
        Assert.NotNull(cut.Find("button[aria-label='Vergrößern']"));
        foreach (var english in new[] { "Undo", "Redo", "Version history", "Close Übersicht", "Expand artifact" })
            Assert.DoesNotContain($"\"{english}\"", cut.Markup);
    }

    [Fact]
    public void Workspace_versions_and_announcements_speak_the_hosts_language()
    {
        var workspace = new CanvasWorkspace();
        var view = workspace.Open("Übersicht");
        view.Spec = TwoStats();
        var texts = CanvasTexts.Default with
        {
            VersionLabel = n => $"Fassung {n}",
            Undone = label => $"Rückgängig: {label ?? "letzte Änderung"}",
            Redone = label => $"Wiederholt: {label ?? "letzte Änderung"}",
            Restored = (n, count) => $"Fassung {n} von {count} wiederhergestellt",
        };

        var cut = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, workspace)
            .Add(x => x.ShowHistory, true)
            .Add(x => x.Texts, texts)
            .Add(x => x.Revision, 1));

        view.Spec = Parse("""{ "root": { "id": "root", "type": "stack", "children": [] } }""");
        cut.Render(p => p.Add(x => x.Revision, 2));

        Assert.Equal("Fassung 2", workspace.Active!.History.Entries[^1].Label);

        cut.Find(".ws-tools button[aria-label='Undo']").Click();
        cut.WaitForAssertion(() => Assert.Equal("Rückgängig: Fassung 2", cut.Find(".ws-live").TextContent.Trim()));

        cut.Find(".ws-tools button[aria-label='Redo']").Click();
        cut.WaitForAssertion(() => Assert.Equal("Wiederholt: Fassung 2", cut.Find(".ws-live").TextContent.Trim()));

        cut.Find(".ws-tools button[aria-label='Version history']").Click();
        cut.FindAll(".ws-version")[^1].Click();   // the oldest version
        cut.WaitForAssertion(() => Assert.Equal("Fassung 1 von 2 wiederhergestellt", cut.Find(".ws-live").TextContent.Trim()));
    }
}
