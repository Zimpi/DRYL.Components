using System.Text.Json;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

public class CanvasLabelTests
{
    private static CanvasNode Node(string type, string props) => new()
    {
        Id = "n1",
        Type = type,
        Props = JsonSerializer.Deserialize<JsonElement>(props),
    };

    [Fact]
    public void Prefers_title_over_everything_else()
    {
        var node = Node("lineChart", """{ "title": "Revenue by month", "label": "ignored" }""");
        Assert.Equal("Revenue by month", CanvasLabel.For(node));
    }

    [Theory]
    [InlineData("""{ "label": "Revenue" }""", "Revenue")]
    [InlineData("""{ "text": "Overdue" }""", "Overdue")]
    [InlineData("""{ "submitLabel": "Approve order" }""", "Approve order")]
    [InlineData("""{ "name": "region" }""", "region")]
    public void Falls_through_the_prop_order(string props, string expected)
    {
        Assert.Equal(expected, CanvasLabel.For(Node("stat", props)));
    }

    [Fact]
    public void Uses_the_first_line_of_markdown_content()
    {
        var node = Node("markdown", """{ "content": "## Summary\nrest of it" }""");
        Assert.Equal("## Summary", CanvasLabel.For(node));
    }

    [Fact]
    public void Falls_back_to_a_readable_type_name()
    {
        Assert.Equal("Line chart", CanvasLabel.For(Node("lineChart", "{}")));
        Assert.Equal("Key value", CanvasLabel.For(Node("keyValue", "{}")));
        Assert.Equal("Divider", CanvasLabel.For(new CanvasNode { Id = "d", Type = "divider" }));
    }

    [Fact]
    public void Truncates_long_labels_to_sixty_characters()
    {
        var node = Node("card", $$"""{ "title": "{{new string('x', 90)}}" }""");
        var label = CanvasLabel.For(node);
        Assert.Equal(60, label.Length);
        Assert.EndsWith("…", label);
    }

    [Fact]
    public void Ignores_blank_props()
    {
        Assert.Equal("Stat", CanvasLabel.For(Node("stat", """{ "label": "   " }""")));
    }
}
