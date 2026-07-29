using DRYL.Components;                       // AddDrylComponents
using DRYL.Components.Agents;                // AddDrylAgents, DrylAgentRunner
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests.Agents;

public class AddDrylAgentsTests
{
    [Fact]
    public void AddDrylAgents_registers_runner_as_scoped()
    {
        var services = new ServiceCollection();
        services.AddDrylComponents();   // core services the runner builds on
        services.AddDrylAgents();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var runner = scope.ServiceProvider.GetService<DrylAgentRunner>();
        Assert.NotNull(runner);

        // Scoped: a different scope yields a different instance.
        using var scope2 = provider.CreateScope();
        Assert.NotSame(runner, scope2.ServiceProvider.GetService<DrylAgentRunner>());
    }

    [Fact]
    public void AddDrylAgents_registers_the_voice_runner_as_scoped()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.JSInterop.IJSRuntime>(
            new DRYL.Components.Tests.Agents.Voice.NoopJsRuntime());
        services.AddDrylComponents();
        services.AddDrylAgents();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var runner = scope.ServiceProvider.GetService<DrylVoiceRunner>();
        Assert.NotNull(runner);

        using var scope2 = provider.CreateScope();
        Assert.NotSame(runner, scope2.ServiceProvider.GetService<DrylVoiceRunner>());
    }
}
