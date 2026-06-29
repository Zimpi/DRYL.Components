using DRYL.Components;

namespace DRYL.Components.Tests;

public class CommandResolutionTests
{
    private sealed class StubResolver : ICommandResolver
    {
        public Task<CommandResolution?> ResolveAsync(
            string query, IReadOnlyList<DrylCommand> commands, CancellationToken ct)
        {
            var cmd = commands[0];
            var args = new Dictionary<string, object?> { ["status"] = "Paid" };
            return Task.FromResult<CommandResolution?>(new CommandResolution(cmd, args, 0.9));
        }
    }

    [Fact]
    public async Task Resolver_returns_command_args_and_confidence()
    {
        ICommandResolver resolver = new StubResolver();
        var commands = new[] { new DrylCommand { Title = "Status setzen" } };
        var res = await resolver.ResolveAsync("markiere als bezahlt", commands, default);

        Assert.NotNull(res);
        Assert.Equal("Status setzen", res!.Command.Title);
        Assert.Equal("Paid", res.Arguments["status"]);
        Assert.Equal(0.9, res.Confidence);
    }
}
