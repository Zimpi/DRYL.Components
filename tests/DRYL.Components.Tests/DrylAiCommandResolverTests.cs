using System.Text.Json;
using DRYL.Components;
using DRYL.Components.Agents;

namespace DRYL.Components.Tests;

public class DrylAiCommandResolverTests
{
    [Fact]
    public void ParseArguments_coerces_by_arg_type()
    {
        var cmd = new DrylCommand { Title = "Status setzen" };
        cmd.AddArgument(new DrylCommandArgument { Name = "status", Type = CommandArgType.Choice });
        cmd.AddArgument(new DrylCommandArgument { Name = "count", Type = CommandArgType.Number });
        cmd.AddArgument(new DrylCommandArgument { Name = "force", Type = CommandArgType.Boolean });

        var json = JsonSerializer.Deserialize<JsonElement>(
            """{"status":"Paid","count":3,"force":true}""");

        var args = DrylAiCommandResolver.ParseArguments(cmd, json);

        Assert.Equal("Paid", args["status"]);
        Assert.Equal(3d, Convert.ToDouble(args["count"]));
        Assert.Equal(true, args["force"]);
    }
}
