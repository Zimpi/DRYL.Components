using System.Text.Json.Nodes;
using DRYL.Components.Agents.Generation;

namespace DRYL.Components.Tests.Agents;

public class JsonMergeTests
{
    private static JsonNode P(string json) => JsonNode.Parse(json)!;

    [Fact]
    public void Merge_into_null_target_returns_patch()
    {
        var result = JsonMerge.Merge(null, P("""{"title":"A"}"""));
        Assert.Equal("A", result!["title"]!.GetValue<string>());
    }

    [Fact]
    public void Scalar_field_in_patch_overwrites_existing()
    {
        var result = JsonMerge.Merge(P("""{"title":"old"}"""), P("""{"title":"new"}"""));
        Assert.Equal("new", result!["title"]!.GetValue<string>());
    }

    [Fact]
    public void Field_absent_from_patch_is_left_untouched()
    {
        var result = JsonMerge.Merge(P("""{"title":"keep","note":"x"}"""), P("""{"note":"y"}"""));
        Assert.Equal("keep", result!["title"]!.GetValue<string>());
        Assert.Equal("y", result["note"]!.GetValue<string>());
    }

    [Fact]
    public void Null_value_in_patch_leaves_existing()
    {
        var result = JsonMerge.Merge(P("""{"title":"keep"}"""), P("""{"title":null}"""));
        Assert.Equal("keep", result!["title"]!.GetValue<string>());
    }

    [Fact]
    public void Array_in_patch_replaces_whole_array()
    {
        var result = JsonMerge.Merge(P("""{"steps":["a","b","c"]}"""), P("""{"steps":["x"]}"""));
        Assert.Single(result!["steps"]!.AsArray());
        Assert.Equal("x", result["steps"]![0]!.GetValue<string>());
    }

    [Fact]
    public void Nested_object_is_merged_recursively()
    {
        var result = JsonMerge.Merge(P("""{"meta":{"a":1,"b":2}}"""), P("""{"meta":{"b":9,"c":3}}"""));
        Assert.Equal(1, result!["meta"]!["a"]!.GetValue<int>());
        Assert.Equal(9, result["meta"]!["b"]!.GetValue<int>());
        Assert.Equal(3, result["meta"]!["c"]!.GetValue<int>());
    }
}
