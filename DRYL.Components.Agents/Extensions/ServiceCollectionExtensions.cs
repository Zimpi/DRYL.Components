using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Agents;

/// <summary>DI helpers for registering DRYL.Components.Agents services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register the DRYL Agents services. Call alongside <c>AddDrylComponents()</c>:
    /// <code>builder.Services.AddDrylComponents().AddDrylAgents();</code>
    /// Registers <see cref="DrylAgentRunner"/> as scoped (one per Blazor circuit).
    /// </summary>
    public static IServiceCollection AddDrylAgents(this IServiceCollection services)
    {
        services.AddScoped<DrylAgentRunner>();
        return services;
    }
}
