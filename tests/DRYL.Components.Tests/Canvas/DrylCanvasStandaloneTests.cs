using System.Text.Json;
using Bunit;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>
/// Pins the point of the A1 move: the renderer is core. Everything here renders a spec without
/// a run, without a generator and without touching DRYL.Components.Agents at all — a line-of-
/// business app may hand the canvas a spec from code, a database or a saved document.
/// </summary>
public class DrylCanvasStandaloneTests : BunitContext
{
    public DrylCanvasStandaloneTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static CanvasSpec Parse(string json) =>
        JsonSerializer.Deserialize<CanvasSpec>(json, CanvasJson.Options)!;

    [Fact]
    public void Renders_a_spec_without_any_agents_type()
    {
        var spec = Parse("""
            {"title":"Report","root":{"id":"root","type":"stack","children":[
                {"id":"s1","type":"stat","props":{"label":"Revenue","value":"€10k"}}]}}
            """);

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, spec));

        Assert.Contains("Revenue", cut.Markup);
        Assert.Contains("Report", cut.Markup);   // title in the header
        Assert.Single(cut.FindAll(".stat"));
    }

    [Fact]
    public void Shows_the_empty_state_without_a_spec()
    {
        var cut = Render<DrylCanvas>(p => p.Add(x => x.EmptyText, "Nothing here."));

        Assert.Contains("Nothing here.", cut.Markup);
    }

    [Fact]
    public void Renders_a_fatal_error_instead_of_the_tree()
    {
        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Parse("""{"root":{"id":"r","type":"divider"}}"""))
            .Add(x => x.Error, "the generator gave up"));

        Assert.Contains("the generator gave up", cut.Markup);
        Assert.Empty(cut.FindAll(".divider"));
    }

    [Fact]
    public void Raises_an_interaction_when_a_button_node_is_clicked()
    {
        CanvasInteraction? seen = null;
        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Parse("""
                {"root":{"id":"root","type":"stack","children":[
                    {"id":"f","type":"inputText","props":{"name":"note","label":"Note","value":"hi"}},
                    {"id":"b","type":"button","props":{"label":"Go","intent":"go"}}]}}
                """))
            .Add(x => x.OnInteraction, i => seen = i));

        cut.Find("[data-cid='b'] button").Click();

        Assert.NotNull(seen);
        Assert.Equal("go", seen!.Intent);
        Assert.Equal("hi", seen.Values["note"]);   // the form snapshot travels with the intent
    }

    [Fact]
    public void Hides_the_refresh_button_when_nothing_is_bound()
    {
        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Parse("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"s1","type":"stat","props":{"label":"Revenue","value":"€10k"}}]}}
            """)));

        Assert.DoesNotContain("Refresh data", cut.Markup);
    }

    [Fact]
    public void Clearing_the_epoch_wipes_user_input_from_the_previous_artifact()
    {
        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Parse("""
                {"root":{"id":"root","type":"stack","children":[
                    {"id":"f","type":"inputText","props":{"name":"note","label":"Note"}}]}}
                """)));

        cut.Instance.Context.Form.Set("note", "typed by the user");
        Assert.Equal("typed by the user", cut.Instance.Context.Form.Get<string>("note"));

        cut.Render(p => p.Add(x => x.Epoch, 1));

        Assert.Null(cut.Instance.Context.Form.Get<string>("note"));
    }
}
