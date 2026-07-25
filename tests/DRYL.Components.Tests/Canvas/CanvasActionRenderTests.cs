using System.Text.Json;
using Bunit;
using DRYL.Components.Canvas;
using DRYL.Components.Dialogs;
using DRYL.Components.Toasts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

public class CanvasActionRenderTests : BunitContext
{
    public CanvasActionRenderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
        // The real providers are components; the runner only ever talks to the services.
        Services.AddScoped<IDrylDialogService, StubDialogService>();
        Services.AddScoped<IDrylToastService, StubToastService>();
    }

    private const string SpecWithAction = """
        {"title":"Aufträge","root":{"id":"r","type":"stack","children":[
          {"id":"btn","type":"button","props":{"label":"Freigeben","kind":"danger"},
           "action":{"name":"order.approve","args":{"orderId":"4711"}}}]}}
        """;

    private static CanvasSpec Parse(string json) =>
        JsonSerializer.Deserialize<CanvasSpec>(json, CanvasJson.Options)!;

    private void Action(Func<ApproveArgs, CanvasActionResult> handler) =>
        Services.AddDrylCanvasAction("order.approve", "Gibt einen Auftrag frei.",
            (ApproveArgs a, CanvasActionContext c, CancellationToken t) => Task.FromResult(handler(a)));

    [Fact]
    public void A_failed_action_shows_the_inline_error()
    {
        Action(_ => CanvasActionResult.Fail("Bereits freigegeben."));

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse(SpecWithAction)));
        cut.Find(".canvas-body button.btn").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Bereits freigegeben.", cut.Find(".canvas-action-error").TextContent));
    }

    [Fact]
    public void A_successful_action_renders_no_inline_error_and_toasts()
    {
        Action(_ => CanvasActionResult.Ok("Freigegeben"));

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse(SpecWithAction)));
        cut.Find(".canvas-body button.btn").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".canvas-action-error"));
            Assert.Equal(new[] { "Freigegeben" },
                ((StubToastService)Services.GetRequiredService<IDrylToastService>()).Successes);
        });
    }

    [Fact]
    public void OnAction_fires_with_the_outcome()
    {
        CanvasActionOutcome? outcome = null;
        Action(_ => CanvasActionResult.Ok("Freigegeben"));

        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Parse(SpecWithAction))
            .Add(x => x.OnAction, (CanvasActionOutcome o) => outcome = o));
        cut.Find(".canvas-body button.btn").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(outcome);
            Assert.True(outcome!.Succeeded);
            Assert.Equal("order.approve", outcome.Action);
            Assert.Equal("btn", outcome.NodeId);
        });
    }

    private const string SpecWithForm = """
        {"title":"Neu","root":{"id":"f","type":"form",
          "props":{"submitLabel":"Anlegen","required":["customer"]},
          "action":{"name":"order.approve","args":{"orderId":{"$field":"customer"}}},
          "children":[{"id":"f1","type":"inputText","props":{"name":"customer","label":"Kunde"}}]}}
        """;

    [Fact]
    public void Form_submit_with_empty_required_field_shows_hint_and_does_not_invoke()
    {
        var invoked = false;
        Action(_ => { invoked = true; return CanvasActionResult.Ok(); });

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse(SpecWithForm)));
        cut.Find(".canvas-form-submit button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.False(invoked);
            Assert.Contains("canvas-field-required", cut.Markup);
        });
    }

    [Fact]
    public void Form_submit_with_filled_required_field_invokes_action()
    {
        var invoked = false;
        Action(_ => { invoked = true; return CanvasActionResult.Ok(); });

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse(SpecWithForm)));
        cut.Find("[data-cid='f1'] input").Input("ACME");
        cut.Find(".canvas-form-submit button").Click();

        cut.WaitForAssertion(() => Assert.True(invoked));
    }

    [Fact]
    public void Typing_into_a_flagged_required_field_clears_the_hint()
    {
        Action(_ => CanvasActionResult.Ok());

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse(SpecWithForm)));
        cut.Find(".canvas-form-submit button").Click();
        cut.WaitForAssertion(() => Assert.Contains("canvas-field-required", cut.Markup));

        cut.Find("[data-cid='f1'] input").Input("ACME");

        // The DrylPresence hint starts its exit as soon as the flag clears. bUnit never fires
        // animationend, so assert the exit phase rather than the removal.
        cut.WaitForAssertion(() =>
            Assert.NotEmpty(cut.FindAll(".presence-exit .canvas-field-required")));
    }

    [Fact]
    public void Kind_danger_renders_the_danger_variant()
    {
        Action(_ => CanvasActionResult.Ok());

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse(SpecWithAction)));

        Assert.Contains("btn-danger", cut.Find(".canvas-body button.btn").GetAttribute("class"));
    }

    [Fact]
    public void A_plain_intent_button_still_raises_OnInteraction()
    {
        CanvasInteraction? raised = null;

        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Parse("""
                {"title":"x","root":{"id":"r","type":"stack","children":[
                  {"id":"btn","type":"button","props":{"label":"Mehr","intent":"more"}}]}}
                """))
            .Add(x => x.OnInteraction, (CanvasInteraction i) => raised = i));
        cut.Find(".canvas-body button.btn").Click();

        cut.WaitForAssertion(() => Assert.Equal("more", raised!.Intent));
    }

    // A host without a single registered action must see exactly what it saw before.
    [Fact]
    public void An_action_button_without_a_registry_falls_back_to_its_intent()
    {
        CanvasInteraction? raised = null;

        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Parse("""
                {"title":"x","root":{"id":"r","type":"stack","children":[
                  {"id":"btn","type":"button","props":{"label":"Freigeben","intent":"approve"},
                   "action":{"name":"order.approve"}}]}}
                """))
            .Add(x => x.OnInteraction, (CanvasInteraction i) => raised = i));
        cut.Find(".canvas-body button.btn").Click();

        cut.WaitForAssertion(() => Assert.Equal("approve", raised!.Intent));
    }

    [Fact]
    public void An_AskAi_result_reaches_OnInteraction_verbatim()
    {
        CanvasInteraction? raised = null;
        Action(_ => CanvasActionResult.Ok().AskAi("Auftrag 4711 wurde freigegeben."));

        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Parse(SpecWithAction))
            .Add(x => x.OnInteraction, (CanvasInteraction i) => raised = i));
        cut.Find(".canvas-body button.btn").Click();

        cut.WaitForAssertion(() =>
            Assert.Equal("Auftrag 4711 wurde freigegeben.", raised!.ToPromptMessage()));
    }

    [Fact]
    public void A_patch_op_from_an_action_reaches_the_spec()
    {
        Action(_ => CanvasActionResult.Ok().Patch(new CanvasOp
        {
            Op = "setProps",
            Id = "state",
            Props = JsonDocument.Parse("""{"text":"Freigegeben","kind":"success"}""").RootElement.Clone(),
        }));

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse("""
            {"title":"x","root":{"id":"r","type":"stack","children":[
              {"id":"state","type":"badge","props":{"text":"Offen","kind":"warning"}},
              {"id":"btn","type":"button","props":{"label":"Freigeben"},
               "action":{"name":"order.approve","args":{"orderId":"4711"}}}]}}
            """)));
        cut.Find(".canvas-body button.btn").Click();

        cut.WaitForAssertion(() => Assert.Contains("Freigegeben", cut.Markup));
    }

    // A real confirmation resolves asynchronously, and the continuation after that await used to
    // run off the Blazor dispatcher — where touching a component (the toast provider, OnAction,
    // the canvas's own render) kills the circuit. This drives the whole sequence through the real
    // component path with a yielding dialog.
    [Fact]
    public void An_async_confirmation_completes_the_whole_sequence()
    {
        CanvasActionOutcome? outcome = null;
        Action(_ => CanvasActionResult.Ok("Freigegeben"));
        ((StubDialogService)Services.GetRequiredService<IDrylDialogService>()).Yield = true;

        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Parse("""
                {"title":"x","root":{"id":"r","type":"stack","children":[
                  {"id":"btn","type":"button","props":{"label":"Freigeben","kind":"danger"},
                   "action":{"name":"order.approve","args":{"orderId":"4711"},
                             "confirm":"Wirklich?"}}]}}
                """))
            .Add(x => x.OnAction, (CanvasActionOutcome o) => outcome = o));
        cut.Find(".canvas-body button.btn").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(outcome);
            Assert.True(outcome!.Succeeded);
            Assert.Equal(new[] { "Freigegeben" },
                ((StubToastService)Services.GetRequiredService<IDrylToastService>()).Successes);
            Assert.Empty(cut.FindAll(".canvas-action-error"));
        });
    }

    // The confirmation gate is part of the render path, not only of the runner: the button must
    // stay untouched when the user says no.
    [Fact]
    public void A_declined_confirmation_leaves_the_artifact_alone()
    {
        var calls = 0;
        Services.AddDrylCanvasAction("order.approve", "…",
            (ApproveArgs a, CanvasActionContext c, CancellationToken t) =>
            {
                calls++;
                return Task.FromResult(CanvasActionResult.Ok("nope"));
            });

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse("""
            {"title":"x","root":{"id":"r","type":"stack","children":[
              {"id":"btn","type":"button","props":{"label":"Freigeben"},
               "action":{"name":"order.approve","args":{"orderId":"4711"},"confirm":"Wirklich?"}}]}}
            """)));
        ((StubDialogService)Services.GetRequiredService<IDrylDialogService>()).Confirm = false;
        cut.Find(".canvas-body button.btn").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(0, calls);
            Assert.Empty(cut.FindAll(".canvas-action-error"));
        });
    }
}
