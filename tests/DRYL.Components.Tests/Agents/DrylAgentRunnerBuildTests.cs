using System.Text.Json;
using DRYL.Components.Agents;

namespace DRYL.Components.Tests.Agents;

public class DrylAgentRunnerBuildTests
{
    private sealed class Recipe { public string? Title { get; set; } }

    [Fact]
    public void CreateUpdateTool_default_name_is_update_lowercased_type()
    {
        var run = new DrylArtifactRun<Recipe>(new JsonSerializerOptions());
        var tool = DrylAgentRunner.CreateUpdateTool(run, new DrylBuildOptions());
        Assert.Equal("update_recipe", tool.Name);
    }

    [Fact]
    public void CreateUpdateTool_honours_a_custom_name()
    {
        var run = new DrylArtifactRun<Recipe>(new JsonSerializerOptions());
        var tool = DrylAgentRunner.CreateUpdateTool(run, new DrylBuildOptions { UpdateToolName = "draft" });
        Assert.Equal("draft", tool.Name);
    }

    [Fact]
    public void CreateUpdateTool_strips_generic_arity_from_default_name()
    {
        var run = new DrylArtifactRun<List<Recipe>>(new JsonSerializerOptions());
        var tool = DrylAgentRunner.CreateUpdateTool(run, new DrylBuildOptions());
        Assert.Equal("update_list", tool.Name);
        Assert.DoesNotContain("`", tool.Name);
    }
}
