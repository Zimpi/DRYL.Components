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
}
