using DRYL.Components.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components;

/// <summary>
/// DI helpers for registering DRYL.Components services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register DRYL component services — currently <see cref="IDrylDialogService"/>.
    /// Call this in <c>Program.cs</c>:
    /// <code>builder.Services.AddDrylComponents();</code>
    /// Then place a single <c>&lt;DrylDialogProvider/&gt;</c> in your root layout.
    /// </summary>
    public static IServiceCollection AddDrylComponents(this IServiceCollection services)
    {
        services.AddScoped<IDrylDialogService, DrylDialogService>();
        return services;
    }
}
