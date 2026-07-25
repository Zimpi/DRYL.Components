using System.Text.Json;
using DRYL.Components.Canvas;

namespace DRYL.Components.Tests.Canvas;

public class CanvasActionValidationTests
{
    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement.Clone();

    private static CanvasValidationContext Context(params string[] fields) => new()
    {
        Actions = new[]
        {
            new CanvasActionDescriptor("order.approve", "Gibt einen Auftrag frei.", new[]
            {
                new CanvasParamInfo("orderId", "string", true),
                new CanvasParamInfo("note", "string", false),
            }),
            new CanvasActionDescriptor("cache.clear", "Leert den Cache.", Array.Empty<CanvasParamInfo>()),
        },
        FieldNames = fields,
    };

    private static CanvasNode Button(string? props, string? action) => new()
    {
        Id = "b",
        Type = "button",
        Props = Json(props ?? """{"label":"Freigeben","intent":"approve"}"""),
        Action = action is null
            ? null
            : JsonSerializer.Deserialize<CanvasActionBinding>(action, CanvasJson.Options),
    };

    [Fact]
    public void A_valid_action_button_passes()
    {
        var node = Button("""{"label":"Freigeben","kind":"danger"}""",
                          """{"name":"order.approve","args":{"orderId":"4711"},"confirm":"Wirklich?"}""");

        Assert.Null(CanvasCatalog.Validate(node, Context()));
    }

    [Fact]
    public void An_unknown_action_names_the_available_ones()
    {
        var node = Button(null, """{"name":"order.nope","args":{}}""");

        var error = CanvasCatalog.Validate(node, Context());

        Assert.Contains("unknown action 'order.nope'", error);
        Assert.Contains("order.approve", error);
    }

    [Fact]
    public void A_missing_required_arg_is_reported_with_the_signature()
    {
        var node = Button(null, """{"name":"order.approve","args":{"note":"x"}}""");

        var error = CanvasCatalog.Validate(node, Context());

        Assert.Contains("missing required arg", error);
        Assert.Contains("orderId", error);
    }

    [Fact]
    public void An_unknown_arg_is_reported()
    {
        var node = Button(null, """{"name":"order.approve","args":{"orderId":"1","nope":2}}""");

        Assert.Contains("no argument 'nope'", CanvasCatalog.Validate(node, Context()));
    }

    [Fact]
    public void A_field_reference_must_point_at_an_interactive_node_of_this_artifact()
    {
        var node = Button(null, """{"name":"order.approve","args":{"orderId":{"$field":"order"}}}""");

        Assert.Null(CanvasCatalog.Validate(node, Context("order")));
        Assert.Contains("references field 'order'", CanvasCatalog.Validate(node, Context("other")));
    }

    [Fact]
    public void An_action_on_a_non_button_is_rejected()
    {
        var node = new CanvasNode
        {
            Id = "s",
            Type = "stat",
            Props = Json("""{"label":"Umsatz","value":"10k"}"""),
            Action = JsonSerializer.Deserialize<CanvasActionBinding>(
                """{"name":"cache.clear"}""", CanvasJson.Options),
        };

        Assert.Contains("can only sit on a button", CanvasCatalog.Validate(node, Context()));
    }

    [Fact]
    public void An_empty_confirm_is_rejected()
    {
        var node = Button(null, """{"name":"cache.clear","confirm":"  "}""");

        Assert.Contains("confirm", CanvasCatalog.Validate(node, Context()));
    }

    [Fact]
    public void A_button_needs_an_intent_or_an_action()
    {
        Assert.Contains("intent or an action",
            CanvasCatalog.Validate(Button("""{"label":"Freigeben"}""", null), Context()));

        Assert.Null(CanvasCatalog.Validate(
            Button("""{"label":"Freigeben"}""", """{"name":"cache.clear"}"""), Context()));
    }

    [Fact]
    public void Kind_danger_is_accepted_and_a_bogus_kind_is_not()
    {
        Assert.Null(CanvasCatalog.Validate(
            Button("""{"label":"Löschen","intent":"delete","kind":"danger"}""", null)));

        Assert.Contains("kind 'ghost' is invalid", CanvasCatalog.Validate(
            Button("""{"label":"Löschen","intent":"delete","kind":"ghost"}""", null)));
    }

    // Without a context nothing about actions is checked — a plain intent button is unchanged.
    [Fact]
    public void Without_a_context_the_old_behaviour_is_preserved()
    {
        Assert.Null(CanvasCatalog.Validate(Button(null, null)));
        Assert.Null(CanvasCatalog.Validate(Button(null, """{"name":"whatever"}""")));
    }

    // A node may carry both a data binding and an action; neither check may swallow the other.
    [Fact]
    public void Data_binding_validation_still_runs_alongside_actions()
    {
        var node = new CanvasNode
        {
            Id = "c",
            Type = "lineChart",
            Props = Json("""{"title":"Umsatz"}"""),
            Data = JsonSerializer.Deserialize<CanvasDataBinding>(
                """{"source":"nope"}""", CanvasJson.Options),
        };

        Assert.Contains("unknown data source 'nope'", CanvasCatalog.Validate(node, Context()));
    }
}
