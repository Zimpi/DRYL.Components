using DRYL.Components;

namespace DRYL.Components.Tests;

public class CommandContextTests
{
    [Fact]
    public void GetArgument_returns_typed_value()
    {
        var ctx = new CommandContext(new Dictionary<string, object?> { ["status"] = "Paid" });
        Assert.Equal("Paid", ctx.GetArgument<string>("status"));
    }

    [Fact]
    public void GetArgument_converts_string_to_number_invariantly()
    {
        var ctx = new CommandContext(new Dictionary<string, object?> { ["amount"] = "0.5" });
        Assert.Equal(0.5d, ctx.GetArgument<double>("amount"));
    }

    [Fact]
    public void GetArgument_missing_returns_default()
    {
        var ctx = new CommandContext(new Dictionary<string, object?>());
        Assert.Null(ctx.GetArgument<string>("nope"));
        Assert.Equal(0, ctx.GetArgument<int>("nope"));
    }
}
