using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

public class DrylCommandRegistryTests : BunitContext
{
    public DrylCommandRegistryTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Same_id_registers_once()
    {
        var reg = new CommandRegistry();
        Render<DrylCommand>(ps => ps.AddCascadingValue<ICommandRegistry>(reg)
            .Add(p => p.Title, "A").Add(p => p.Id, "dup"));
        Render<DrylCommand>(ps => ps.AddCascadingValue<ICommandRegistry>(reg)
            .Add(p => p.Title, "B").Add(p => p.Id, "dup"));
        Assert.Single(reg.Commands);
    }

    [Fact]
    public void DrylCommand_self_registers_into_cascaded_registry()
    {
        var reg = new CommandRegistry();
        Render<DrylCommand>(ps => ps
            .AddCascadingValue<ICommandRegistry>(reg)
            .Add(p => p.Title, "Neue Rechnung"));

        Assert.Single(reg.Commands);
        Assert.Equal("Neue Rechnung", reg.Commands[0].Title);
    }

    [Fact]
    public void DrylCommandArgument_registers_into_parent_command()
    {
        var reg = new CommandRegistry();
        Render<DrylCommand>(ps => ps
            .AddCascadingValue<ICommandRegistry>(reg)
            .Add(p => p.Title, "Status setzen")
            .AddChildContent<DrylCommandArgument>(a => a
                .Add(p => p.Name, "status")
                .Add(p => p.Type, CommandArgType.Choice)
                .Add(p => p.Options, new[] { "Draft", "Paid" })));

        var cmd = reg.Commands.Single();
        Assert.Single(cmd.Arguments);
        Assert.Equal("status", cmd.Arguments[0].Name);
    }
}
