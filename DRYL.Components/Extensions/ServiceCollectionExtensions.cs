using DRYL.Components.Dialogs;
using DRYL.Components.Toasts;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components;

/// <summary>
/// DI helpers for registering DRYL.Components services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register DRYL component services — <see cref="IDrylDialogService"/> and
    /// <see cref="IDrylToastService"/>.
    /// Call this in <c>Program.cs</c>:
    /// <code>builder.Services.AddDrylComponents();</code>
    /// Then place a single <c>&lt;DrylDialogProvider/&gt;</c> and (if you want
    /// service-driven toasts) a <c>&lt;DrylToastProvider/&gt;</c> in your root layout.
    /// </summary>
    public static IServiceCollection AddDrylComponents(this IServiceCollection services)
    {
        services.AddScoped<IDrylDialogService, DrylDialogService>();
        services.AddScoped<IDrylToastService, DrylToastService>();
        return services;
    }
}
