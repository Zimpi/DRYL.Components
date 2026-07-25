using System.Text.Json;
using DRYL.Components.Canvas;
using DRYL.Components.Agents.Generation;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

public class CanvasSpecTests
{
    private const string Sample = """
        { "title": "Q2", "root": { "id": "root", "type": "stack",
          "props": { "gap": "md" },
          "children": [ { "id": "rev", "type": "stat",
            "props": { "label": "Revenue", "value": "48.2k" } } ] } }
        """;

    [Fact]
    public void Deserializes_camelCase_tree()
    {
        var spec = JsonSerializer.Deserialize<CanvasSpec>(Sample, CanvasJson.Options)!;
        Assert.Equal("Q2", spec.Title);
        Assert.Equal("stack", spec.Root!.Type);
        Assert.Equal("rev", spec.Root.Children![0].Id);
        Assert.Equal("stat", spec.Root.Children[0].Type);
    }

    [Fact]
    public void PartialJsonReader_materializes_nodes_progressively()
    {
        var reader = new PartialJsonReader<CanvasSpec>(CanvasJson.Options);
        // Cut mid-way through the second node's props:
        var cut = Sample.IndexOf("\"value\"", StringComparison.Ordinal);
        var first = reader.Append(Sample[..cut]);
        Assert.NotNull(first?.Root);
        Assert.Equal("root", first!.Root!.Id);          // container already there
        var second = reader.Append(Sample[cut..]);
        Assert.Equal("48.2k", GetProp(second!.Root!.Children![0], "value"));
    }

    [Fact]
    public void Removing_flag_is_not_serialized()
    {
        var node = new CanvasNode { Id = "a", Type = "divider", Removing = true };
        var json = JsonSerializer.Serialize(node, CanvasJson.Options);
        Assert.DoesNotContain("removing", json, StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetProp(CanvasNode n, string name) =>
        n.Props!.Value.TryGetProperty(name, out var v) ? v.GetString() : null;
}
