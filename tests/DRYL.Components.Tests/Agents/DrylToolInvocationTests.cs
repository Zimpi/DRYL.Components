using DRYL.Components;
using DRYL.Components.Agents;

namespace DRYL.Components.Tests.Agents;

public class DrylToolInvocationTests
{
    [Fact]
    public void State_is_Thinking_while_running()
    {
        var t = new DrylToolInvocation { CallId = "1", ToolName = "get_weather" };
        Assert.Equal(AiState.Thinking, t.State);
    }

    [Fact]
    public void State_is_Generated_when_result_set()
    {
        var t = new DrylToolInvocation { CallId = "1", ToolName = "get_weather", Result = "\"sunny\"" };
        Assert.Equal(AiState.Generated, t.State);
    }

    [Fact]
    public void State_is_None_when_error_set()
    {
        var t = new DrylToolInvocation { CallId = "1", ToolName = "x", Error = "boom" };
        Assert.Equal(AiState.None, t.State);
    }
}
