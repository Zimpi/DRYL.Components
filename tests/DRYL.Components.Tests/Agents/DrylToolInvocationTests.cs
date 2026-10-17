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

    [Fact]
    public void Approval_defaults_to_none()
        => Assert.Equal(DrylToolApproval.None, new DrylToolInvocation().Approval);

    [Theory]
    [InlineData(DrylToolApproval.Pending, null, null, AiState.Proposed)]
    [InlineData(DrylToolApproval.Approved, null, null, AiState.Generated)]   // accepted: straight to Generated (the wave)
    [InlineData(DrylToolApproval.Approved, "\"ok\"", null, AiState.Generated)]
    [InlineData(DrylToolApproval.Declined, null, null, AiState.None)]
    [InlineData(DrylToolApproval.Declined, "\"rejected\"", null, AiState.None)]
    [InlineData(DrylToolApproval.Pending, null, "boom", AiState.None)]      // an error always wins
    [InlineData(DrylToolApproval.Approved, null, "boom", AiState.None)]
    [InlineData(DrylToolApproval.None, null, null, AiState.Thinking)]
    [InlineData(DrylToolApproval.None, "\"ok\"", null, AiState.Generated)]
    public void State_follows_the_approval(DrylToolApproval approval, string? result, string? error, AiState expected)
    {
        var call = new DrylToolInvocation { Approval = approval, Result = result, Error = error };

        Assert.Equal(expected, call.State);
    }

    [Theory]
    [InlineData(DrylToolApproval.None, null, null, true)]
    [InlineData(DrylToolApproval.None, "\"ok\"", null, false)]
    [InlineData(DrylToolApproval.Approved, null, null, true)]      // accepted, result not in yet
    [InlineData(DrylToolApproval.Approved, "null", null, false)]   // a void tool's result
    [InlineData(DrylToolApproval.Approved, null, "boom", false)]
    [InlineData(DrylToolApproval.Pending, null, null, false)]      // has not run
    [InlineData(DrylToolApproval.Declined, null, null, false)]
    public void Running_until_the_result_or_error_is_in(DrylToolApproval approval, string? result, string? error, bool expected)
    {
        var call = new DrylToolInvocation { Approval = approval, Result = result, Error = error };

        Assert.Equal(expected, call.Running);
    }
}
