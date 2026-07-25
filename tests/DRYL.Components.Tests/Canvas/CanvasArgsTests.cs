using System.Text.Json;
using DRYL.Components.Canvas;

namespace DRYL.Components.Tests.Canvas;

public class CanvasArgsTests
{
    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement.Clone();

    [Fact]
    public void Literals_pass_through_unchanged()
    {
        var form = new CanvasFormState();

        var resolved = CanvasArgs.Resolve(Json("""{"year":2026,"region":"EMEA"}"""), form, out var fields);

        Assert.Empty(fields);
        Assert.Equal(2026, resolved!.Value.GetProperty("year").GetInt32());
        Assert.Equal("EMEA", resolved.Value.GetProperty("region").GetString());
    }

    [Fact]
    public void Field_reference_reads_the_current_form_value()
    {
        var form = new CanvasFormState();
        form.Set("region", "APAC");

        var resolved = CanvasArgs.Resolve(Json("""{"region":{"$field":"region"}}"""), form, out var fields);

        Assert.Equal(new[] { "region" }, fields);
        Assert.Equal("APAC", resolved!.Value.GetProperty("region").GetString());
    }

    [Fact]
    public void Field_reference_to_an_unset_field_resolves_to_null()
    {
        var resolved = CanvasArgs.Resolve(Json("""{"region":{"$field":"nope"}}"""),
                                          new CanvasFormState(), out _);

        Assert.Equal(JsonValueKind.Null, resolved!.Value.GetProperty("region").ValueKind);
    }

    [Fact]
    public void A_non_object_or_missing_args_resolves_to_null()
    {
        Assert.Null(CanvasArgs.Resolve(null, new CanvasFormState(), out _));
        Assert.Null(CanvasArgs.Resolve(Json("42"), new CanvasFormState(), out _));
    }

    [Fact]
    public void FieldReference_recognises_only_the_dollar_field_shape()
    {
        Assert.Equal("x", CanvasArgs.FieldReference(Json("""{"$field":"x"}""")));
        Assert.Null(CanvasArgs.FieldReference(Json("""{"field":"x"}""")));
        Assert.Null(CanvasArgs.FieldReference(Json("\"x\"")));
    }

    [Fact]
    public void HasFieldReference_detects_a_reference_anywhere_in_the_object()
    {
        Assert.True(CanvasArgs.HasFieldReference(Json("""{"a":1,"b":{"$field":"x"}}""")));
        Assert.False(CanvasArgs.HasFieldReference(Json("""{"a":1}""")));
        Assert.False(CanvasArgs.HasFieldReference(null));
    }

    // The binder and the runner must produce byte-identical JSON for the same input —
    // two copies of this logic would drift and the prompt promises the model one syntax.
    [Fact]
    public void The_same_input_always_canonicalises_to_the_same_key()
    {
        var form = new CanvasFormState();
        form.Set("region", "APAC");
        var raw = Json("""{"year":2026,"region":{"$field":"region"}}""");

        var first = CanvasDataKey.Of("s", CanvasArgs.Resolve(raw, form, out _));
        var second = CanvasDataKey.Of("s", CanvasArgs.Resolve(raw, form, out _));

        Assert.Equal(first, second);
        Assert.Contains("APAC", first);
    }
}
