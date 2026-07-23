using System.Text.Json;
using DRYL.Components.Agents.Generation;

namespace DRYL.Components.Tests.Agents;

public class JsonPartialRepairTests
{
    private static JsonDocument Parse(string s) => JsonDocument.Parse(JsonPartialRepair.Close(s));

    [Fact]
    public void Complete_object_is_unchanged_semantically()
    {
        using var doc = Parse("""{"a":1,"b":"x"}""");
        Assert.Equal(1, doc.RootElement.GetProperty("a").GetInt32());
        Assert.Equal("x", doc.RootElement.GetProperty("b").GetString());
    }

    [Fact]
    public void Unclosed_object_is_closed()
    {
        using var doc = Parse("""{"a":1""");
        Assert.Equal(1, doc.RootElement.GetProperty("a").GetInt32());
    }

    [Fact]
    public void Open_string_is_closed_keeping_partial_content()
    {
        using var doc = Parse("""{"title":"Hel""");
        Assert.Equal("Hel", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public void Unclosed_array_with_half_object_is_closed()
    {
        using var doc = Parse("""{"steps":["a","b""");
        var steps = doc.RootElement.GetProperty("steps");
        Assert.Equal(2, steps.GetArrayLength());
        Assert.Equal("a", steps[0].GetString());
        Assert.Equal("b", steps[1].GetString());
    }

    [Fact]
    public void Half_written_number_keeps_its_complete_numeric_prefix()
    {
        // "12." is incomplete, but dropping the field would flicker price -> null -> value as
        // the rest streams in. The repair keeps the stable prefix "12" instead (shrink-guard).
        using var doc = Parse("""{"name":"x","price":12.""");
        Assert.Equal("x", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal(12, doc.RootElement.GetProperty("price").GetInt32());
    }

    [Fact]
    public void Number_with_no_complete_prefix_is_still_dropped()
    {
        // A bare sign / decimal point has no complete numeric prefix yet — drop the field.
        using var doc = Parse("""{"name":"x","price":-""");
        Assert.Equal("x", doc.RootElement.GetProperty("name").GetString());
        Assert.False(doc.RootElement.TryGetProperty("price", out _));
    }

    [Fact]
    public void Half_written_literal_is_dropped()
    {
        using var doc = Parse("""{"a":1,"ok":tru""");
        Assert.Equal(1, doc.RootElement.GetProperty("a").GetInt32());
        Assert.False(doc.RootElement.TryGetProperty("ok", out _));
    }

    [Fact]
    public void Dangling_key_before_colon_is_dropped()
    {
        using var doc = Parse("""{"a":1,"b""");
        Assert.Equal(1, doc.RootElement.GetProperty("a").GetInt32());
        Assert.False(doc.RootElement.TryGetProperty("b", out _));
    }

    [Fact]
    public void Trailing_comma_is_dropped()
    {
        using var doc = Parse("""{"a":1,""");
        Assert.Equal(1, doc.RootElement.GetProperty("a").GetInt32());
    }
}
