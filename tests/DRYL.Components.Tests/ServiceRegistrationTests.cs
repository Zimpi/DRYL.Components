using DRYL.Components;
using DRYL.Components.Ai;
using DRYL.Components.Dialogs;
using DRYL.Components.Toasts;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests;

/// <summary>
/// Verifies that <c>AddDrylComponents()</c> wires up every DRYL service so a
/// consumer's <c>Program.cs</c> call is enough to use dialogs, toasts,
/// notifications and AI activity orchestration.
/// </summary>
public class ServiceRegistrationTests
{
    [Theory]
    [InlineData(typeof(IDrylDialogService))]
    [InlineData(typeof(IDrylToastService))]
    [InlineData(typeof(IDrylNotificationService))]
    [InlineData(typeof(IDrylAiActivityService))]
    public void AddDrylComponents_registers_service(Type serviceType)
    {
        var services = new ServiceCollection();
        services.AddDrylComponents();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService(serviceType));
    }

    [Fact]
    public void AddDrylComponents_is_chainable()
    {
        var services = new ServiceCollection();

        var returned = services.AddDrylComponents();

        Assert.Same(services, returned);
    }
}
