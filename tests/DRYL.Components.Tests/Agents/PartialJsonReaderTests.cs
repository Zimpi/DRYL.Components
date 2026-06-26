using DRYL.Components.Agents.Generation;

namespace DRYL.Components.Tests.Agents;

public class PartialJsonReaderTests
{
    public sealed class Recipe
    {
        public string? Title { get; set; }
        public List<string> Steps { get; set; } = new();
    }

    [Fact]
    public void Surfaces_partial_title_character_by_character()
    {
        var r = new PartialJsonReader<Recipe>();
        r.Append("{\"title\":\"Pan");
        Assert.Equal("Pan", r.Current!.Title);

        r.Append("cakes\"");
        Assert.Equal("Pancakes", r.Current!.Title);
    }

    [Fact]
    public void Builds_array_incrementally()
    {
        var r = new PartialJsonReader<Recipe>();
        r.Append("{\"title\":\"X\",\"steps\":[\"mix");
        Assert.Single(r.Current!.Steps);
        Assert.Equal("mix", r.Current!.Steps[0]);

        r.Append("\",\"bake\"]}");
        Assert.Equal(2, r.Current!.Steps.Count);
        Assert.Equal("bake", r.Current!.Steps[1]);
    }

    [Fact]
    public void Holds_last_good_snapshot_on_unparseable_intermediate()
    {
        var r = new PartialJsonReader<Recipe>();
        r.Append("{\"title\":\"Done\"");        // -> Title "Done"
        var before = r.Current!.Title;
        r.Append(",\\");                          // garbage-ish continuation
        Assert.Equal(before, r.Current!.Title);   // never regress to null
    }
}
