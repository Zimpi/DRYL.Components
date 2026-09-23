using System.Text.Json;
using Bunit;
using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>The dock is a command bar, not a chat: one input, one status line, the log on demand.</summary>
public class DrylCanvasDockTests : BunitContext
{
    public DrylCanvasDockTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static DrylCanvasRun BuildingRun()
    {
        var spec = JsonSerializer.Deserialize<CanvasSpec>(
            """
            { "title": "Report", "root": { "id": "root", "type": "stack", "children": [
                { "id": "a", "type": "markdown", "props": { "content": "x" } },
                { "id": "b", "type": "markdown", "props": { "content": "y" } } ] } }
            """, CanvasJson.Options)!;

        var run = new DrylCanvasRun();
        run.BeginCreate();
        run.RevealSnapshot(spec);
        return run;
    }

    [Fact]
    public void The_status_line_reports_the_run()
    {
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Run, BuildingRun()));

        Assert.Contains("Building", cut.Find(".dock-status").TextContent);
    }

    [Fact]
    public void An_untouched_run_reads_as_idle()
    {
        // A canvas run exists long before anything asks it for an artifact — a freshly loaded
        // page must not claim the assistant is working.
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Run, new DrylCanvasRun()));

        Assert.Contains("Idle", cut.Find(".dock-status").TextContent);
    }

    [Fact]
    public void A_restored_artifact_reads_as_ready_not_as_working()
    {
        // A document loaded from a store fills Spec without any generation ever running. Claiming
        // work would be a lie nothing can settle: no generation is coming to end it.
        var spec = JsonSerializer.Deserialize<CanvasSpec>(
            """
            { "title": "Report", "root": { "id": "root", "type": "stack", "children": [
                { "id": "a", "type": "markdown", "props": { "content": "x" } } ] } }
            """, CanvasJson.Options)!;

        var workspace = new CanvasWorkspace();
        workspace.Open("Übersicht").Spec = spec;
        var run = new DrylCanvasRun();
        run.UseWorkspace(workspace);

        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Run, run));

        var status = cut.Find(".dock-status").TextContent;
        Assert.Contains("Ready", status);
        Assert.DoesNotContain("Working", status);
    }

    [Fact]
    public void Busy_alone_makes_the_dock_work()
    {
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, new DrylCanvasRun())
            .Add(x => x.Busy, true));

        Assert.Contains("Working", cut.Find(".dock-status").TextContent);
    }

    [Fact]
    public void A_failed_run_puts_its_message_in_the_status_line()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        run.FailGeneration(new InvalidOperationException("generator gave up"));

        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Run, run));

        Assert.Contains("generator gave up", cut.Find(".dock-status").TextContent);
        Assert.Contains("is-error", cut.Find(".dock-status").GetAttribute("class"));
    }

    [Fact]
    public void An_explicit_status_wins()
    {
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, BuildingRun())
            .Add(x => x.Status, "Waiting for approval"));

        Assert.Contains("Waiting for approval", cut.Find(".dock-status").TextContent);
        Assert.DoesNotContain("Building", cut.Find(".dock-status").TextContent);
    }

    [Fact]
    public void Sending_raises_OnSend_with_the_draft()
    {
        string? sent = null;
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.OnSend, EventCallback.Factory.Create<string>(this, s => sent = s)));

        var composer = cut.FindComponent<DrylChatComposer>();
        composer.Find("textarea").Input("open the order view");
        composer.Find("button").Click();

        Assert.Equal("open the order view", sent);
    }

    [Fact]
    public void Busy_disables_the_composer()
    {
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Busy, true));

        Assert.True(cut.FindComponent<DrylChatComposer>().Instance.Disabled);
    }

    [Fact]
    public void Collapsed_leaves_a_single_labelled_button()
    {
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Collapsed, true)
            .Add(x => x.Title, "Assistant"));

        Assert.Empty(cut.FindAll(".dock-panel"));
        Assert.Equal("Assistant", cut.Find(".dock-fab button").GetAttribute("aria-label"));

        cut.Find(".dock-fab button").Click();
        Assert.Single(cut.FindAll(".dock-panel"));
    }

    [Fact]
    public void Without_a_log_there_is_no_disclosure()
    {
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Run, BuildingRun()));

        Assert.Empty(cut.FindAll(".dock-log-toggle"));
        Assert.Empty(cut.FindAll(".dock-log"));
    }

    [Fact]
    public void The_log_slot_renders_and_toggles()
    {
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Log, (RenderFragment)(b => b.AddMarkupContent(0, "<p>turn one</p>"))));

        Assert.Contains("turn one", cut.Markup);
        Assert.Equal("true", cut.Find(".dock-log").GetAttribute("aria-hidden"));

        cut.Find(".dock-log-toggle").Click();
        Assert.Null(cut.Find(".dock-log").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void The_corner_becomes_a_class()
    {
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Corner, DockCorner.TopLeft));

        Assert.Contains("canvas-dock--tl", cut.Find(".canvas-dock").GetAttribute("class"));
    }

    [Fact]
    public void Host_actions_render_in_the_head()
    {
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Actions,
            (RenderFragment)(b => b.AddMarkupContent(0, "<button id=\"stop\">Stop</button>"))));

        Assert.NotNull(cut.Find(".dock-head #stop"));
    }

    [Fact]
    public void Suggestions_render_above_the_composer()
    {
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Suggestions,
            (RenderFragment)(b => b.AddMarkupContent(0, "<button id=\"chip\">Kennzahlen</button>"))));

        Assert.NotNull(cut.Find(".dock-suggestions #chip"));
    }

    [Fact]
    public void Without_the_slots_the_dock_grows_no_containers()
    {
        var cut = Render<DrylCanvasDock>();

        Assert.Empty(cut.FindAll(".dock-actions"));
        Assert.Empty(cut.FindAll(".dock-suggestions"));
    }

    // ── Voice ────────────────────────────────────────────────────────────────

    private static DrylVoiceRun VoiceRun() =>
        new DrylVoiceRunner(new DRYL.Components.Tests.Agents.Voice.NoopJsRuntime())
            .Create(new DrylVoiceOptions { ApiKey = "sk-test" });

    [Fact]
    public void Without_a_voice_run_the_dock_is_exactly_what_it_was()
    {
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Run, new DrylCanvasRun()));

        Assert.Empty(cut.FindAll(".dock-voice-toggle"));
        Assert.Empty(cut.FindAll(".dock-voice"));
        Assert.NotNull(cut.Find(".chat-composer"));
    }

    [Fact]
    public void An_idle_voice_run_offers_a_microphone()
    {
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, new DrylCanvasRun())
            .Add(x => x.Voice, VoiceRun())
            .Add(x => x.VoiceLabel, "Mit dem Assistenten sprechen"));

        var button = cut.Find(".dock-voice-toggle");
        Assert.Equal("Mit dem Assistenten sprechen", button.GetAttribute("aria-label"));
        Assert.NotNull(cut.Find(".chat-composer"));
    }

    [Fact]
    public void A_live_session_takes_the_dock_over()
    {
        var voice = VoiceRun();
        voice.OnConnected();

        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, new DrylCanvasRun())
            .Add(x => x.Voice, voice));

        Assert.NotNull(cut.Find(".dock-voice .voice-orb"));
        Assert.NotNull(cut.Find(".dock-voice-stop"));
        // The way in is gone while the session runs; the way out is the stop button.
        Assert.Empty(cut.FindAll(".dock-voice-toggle"));
        // And the composer is gone: you are talking, not typing.
        Assert.Empty(cut.FindAll(".chat-composer"));
    }

    [Fact]
    public void The_status_line_says_what_the_voice_is_doing()
    {
        var voice = VoiceRun();
        voice.OnConnected();
        voice.OnActivity(nameof(VoiceActivity.Speaking));

        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, new DrylCanvasRun())
            .Add(x => x.Voice, voice));

        Assert.Contains("Speaking", cut.Find(".dock-status").TextContent);
    }

    [Fact]
    public void A_host_status_still_wins_over_the_voice()
    {
        var voice = VoiceRun();
        voice.OnConnected();

        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, new DrylCanvasRun())
            .Add(x => x.Voice, voice)
            .Add(x => x.Status, "Schritt 2 von 3"));

        Assert.Contains("Schritt 2 von 3", cut.Find(".dock-status").TextContent);
    }

    [Fact]
    public void The_last_spoken_line_is_shown_under_the_orb()
    {
        var voice = VoiceRun();
        voice.OnConnected();
        voice.OnTranscript(nameof(VoiceRole.User), "Zeig mir die Projekte");
        voice.OnTranscript(nameof(VoiceRole.Assistant), "Ist gebaut.");

        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, new DrylCanvasRun())
            .Add(x => x.Voice, voice));

        Assert.Contains("Ist gebaut.", cut.Find(".dock-voice-line").TextContent);
    }

    [Fact]
    public void A_voice_failure_is_reported_where_every_other_failure_is()
    {
        var voice = VoiceRun();
        voice.OnFailed("Kein Zugriff auf das Mikrofon.");

        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, new DrylCanvasRun())
            .Add(x => x.Voice, voice));

        var status = cut.Find(".dock-status");
        Assert.Contains("Mikrofon", status.TextContent);
        Assert.Contains("is-error", status.ClassList);
    }

    // ── A failure outranks the host's status (Trello #62) ───────────────────

    [Fact]
    public void A_voice_failure_outranks_a_host_status()
    {
        var voice = VoiceRun();
        voice.OnFailed("Kein Zugriff auf das Mikrofon.");

        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, new DrylCanvasRun())
            .Add(x => x.Voice, voice)
            .Add(x => x.Status, "Bereit"));

        var status = cut.Find(".dock-status");
        Assert.Contains("Mikrofon", status.TextContent);
        Assert.DoesNotContain("Bereit", status.TextContent);
        Assert.Contains("is-error", status.ClassList);
    }

    [Fact]
    public void A_run_failure_outranks_a_host_status()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        run.FailGeneration(new InvalidOperationException("generator gave up"));

        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, run)
            .Add(x => x.Status, "Bereit"));

        Assert.Contains("generator gave up", cut.Find(".dock-status").TextContent);
        Assert.Contains("is-error", cut.Find(".dock-status").ClassList);
    }

    [Fact]
    public void ErrorText_words_the_failure()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        run.FailGeneration(new InvalidOperationException("generator gave up"));

        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, run)
            .Add(x => x.ErrorText, e => "Fehler: " + e.Message));

        Assert.Contains("Fehler: generator gave up", cut.Find(".dock-status").TextContent);
        Assert.Contains("is-error", cut.Find(".dock-status").ClassList);
    }

    [Fact]
    public void A_live_voice_session_outranks_a_stale_run_failure()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        run.FailGeneration(new InvalidOperationException("generator gave up"));
        var voice = VoiceRun();
        voice.OnConnected();

        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, run)
            .Add(x => x.Voice, voice));

        Assert.Contains("Listening", cut.Find(".dock-status").TextContent);
        Assert.DoesNotContain("is-error", cut.Find(".dock-status").ClassList);
    }

    // ── Every label and status line can be localised (Trello #61) ────────────

    [Fact]
    public void The_default_labels_are_unchanged()
    {
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Log, (RenderFragment)(b => b.AddMarkupContent(0, "<p>x</p>"))));

        Assert.Equal("Show conversation", cut.Find(".dock-log-toggle").GetAttribute("aria-label"));
        Assert.Equal("Collapse assistant", cut.Find(".dock-collapse").GetAttribute("aria-label"));
        cut.Find(".dock-log-toggle").Click();
        Assert.Equal("Hide conversation", cut.Find(".dock-log-toggle").GetAttribute("aria-label"));
    }

    [Fact]
    public void The_head_labels_can_be_overridden()
    {
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Log, (RenderFragment)(b => b.AddMarkupContent(0, "<p>x</p>")))
            .Add(x => x.ShowLogLabel, "Verlauf zeigen")
            .Add(x => x.HideLogLabel, "Verlauf verbergen")
            .Add(x => x.CollapseLabel, "Assistent einklappen"));

        Assert.Equal("Verlauf zeigen", cut.Find(".dock-log-toggle").GetAttribute("aria-label"));
        Assert.Equal("Assistent einklappen", cut.Find(".dock-collapse").GetAttribute("aria-label"));
        Assert.Contains("Assistent einklappen", cut.Markup);   // the tooltip text
        cut.Find(".dock-log-toggle").Click();
        Assert.Equal("Verlauf verbergen", cut.Find(".dock-log-toggle").GetAttribute("aria-label"));
    }

    [Fact]
    public void The_clear_context_label_can_be_overridden()
    {
        var selection = new CanvasSelection();
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Selection, selection)
            .Add(x => x.ClearContextLabel, "Bezug entfernen"));
        cut.InvokeAsync(() => selection.Select(new CanvasNode
        {
            Id = "c3", Type = "lineChart",
            Props = JsonSerializer.Deserialize<JsonElement>("""{ "title": "Umsatz" }"""),
        }));

        Assert.NotNull(cut.Find(".dock-context button[aria-label='Bezug entfernen']"));
    }

    [Fact]
    public void The_stop_button_text_can_be_overridden()
    {
        var voice = VoiceRun();
        voice.OnConnected();

        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Voice, voice)
            .Add(x => x.VoiceStopLabel, "Gespräch beenden"));

        Assert.Contains("Gespräch beenden", cut.Find(".dock-voice-stop").TextContent);
    }

    [Fact]
    public void The_default_stop_button_text_is_unchanged()
    {
        var voice = VoiceRun();
        voice.OnConnected();

        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Voice, voice));

        Assert.Contains("End voice session", cut.Find(".dock-voice-stop").TextContent);
    }

    [Fact]
    public void The_status_lines_can_be_overridden()
    {
        var idle = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, new DrylCanvasRun())
            .Add(x => x.IdleText, "Bereit"));
        Assert.Contains("Bereit", idle.Find(".dock-status").TextContent);

        var working = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, new DrylCanvasRun())
            .Add(x => x.Busy, true)
            .Add(x => x.WorkingText, "Arbeitet…"));
        Assert.Contains("Arbeitet…", working.Find(".dock-status").TextContent);

        var building = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, BuildingRun())
            .Add(x => x.BuildingText, n => $"Baut · {n} Elemente"));
        Assert.Matches(@"Baut · \d+ Elemente", building.Find(".dock-status").TextContent);
    }

    [Fact]
    public void ReadyText_words_a_standing_artifact()
    {
        var spec = JsonSerializer.Deserialize<CanvasSpec>(
            """
            { "title": "Report", "root": { "id": "root", "type": "stack", "children": [
                { "id": "a", "type": "markdown", "props": { "content": "x" } } ] } }
            """, CanvasJson.Options)!;
        var workspace = new CanvasWorkspace();
        workspace.Open("Übersicht").Spec = spec;
        var run = new DrylCanvasRun();
        run.UseWorkspace(workspace);

        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, run)
            .Add(x => x.ReadyText, n => $"Fertig · {n}"));

        Assert.Matches(@"Fertig · \d+", cut.Find(".dock-status").TextContent);
    }

    [Fact]
    public void VoiceStatusText_words_the_voice_and_null_falls_through()
    {
        var voice = VoiceRun();
        voice.OnConnected();
        voice.OnActivity(nameof(VoiceActivity.Speaking));

        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Voice, voice)
            .Add(x => x.VoiceStatusText, (phase, activity) =>
                activity == VoiceActivity.Speaking ? "Spricht…" : null));
        Assert.Contains("Spricht…", cut.Find(".dock-status").TextContent);

        voice.OnActivity(nameof(VoiceActivity.Listening));
        cut.Render();
        Assert.Contains("Listening", cut.Find(".dock-status").TextContent);
    }

    // ── One press into a voice session (Trello #63) ─────────────────────────

    /// <summary>A voice run whose token request never answers, so it stays in Connecting.</summary>
    private static DrylVoiceRun PendingVoiceRun() =>
        new DrylVoiceRunner(new DRYL.Components.Tests.Agents.Voice.NoopJsRuntime(), new HttpClient(new NeverAnswers()))
            .Create(new DrylVoiceOptions { ApiKey = "sk-test" });

    private sealed class NeverAnswers : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            new TaskCompletionSource<HttpResponseMessage>().Task.WaitAsync(ct);
    }

    [Fact]
    public void Without_VoiceFirst_the_collapsed_button_only_expands()
    {
        var voice = PendingVoiceRun();
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Collapsed, true)
            .Add(x => x.Voice, voice));

        Assert.Equal("Assistant", cut.Find(".dock-fab button").GetAttribute("aria-label"));
        cut.Find(".dock-fab button").Click();

        Assert.Single(cut.FindAll(".dock-panel"));
        Assert.Equal(VoicePhase.Idle, voice.Phase);
    }

    [Fact]
    public void VoiceFirst_labels_the_collapsed_button_with_the_voice_label()
    {
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Collapsed, true)
            .Add(x => x.Voice, PendingVoiceRun())
            .Add(x => x.VoiceFirst, true)
            .Add(x => x.VoiceLabel, "Mit Santiago sprechen"));

        var button = cut.Find(".dock-fab button");
        Assert.Equal("Mit Santiago sprechen", button.GetAttribute("aria-label"));
        Assert.Equal("button", button.TagName.ToLowerInvariant());   // native: Enter and Space press it
        Assert.Equal("Microphone", cut.FindComponent<DrylIcon>().Instance.Name);
    }

    [Fact]
    public void VoiceFirst_expands_and_starts_the_session_in_one_press()
    {
        var voice = PendingVoiceRun();
        bool? collapsed = null;
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Collapsed, true)
            .Add(x => x.CollapsedChanged, c => collapsed = c)
            .Add(x => x.Voice, voice)
            .Add(x => x.VoiceFirst, true));

        cut.Find(".dock-fab button").Click();

        Assert.False(collapsed);
        Assert.Equal(VoicePhase.Connecting, voice.Phase);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".dock-voice .voice-orb")));
    }

    [Fact]
    public void VoiceFirst_without_a_voice_run_changes_nothing()
    {
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Collapsed, true)
            .Add(x => x.VoiceFirst, true));

        Assert.Equal("Assistant", cut.Find(".dock-fab button").GetAttribute("aria-label"));
        cut.Find(".dock-fab button").Click();
        Assert.Single(cut.FindAll(".dock-panel"));
    }

    [Fact]
    public void CollapsedIcon_replaces_the_glyph()
    {
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Collapsed, true)
            .Add(x => x.CollapsedIcon, "MapPin"));

        Assert.Equal("MapPin", cut.FindComponent<DrylIcon>().Instance.Name);
    }
}
