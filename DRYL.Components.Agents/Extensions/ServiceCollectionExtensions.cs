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

    /// <summary>
    /// Register a <see cref="DRYL.Components.ICommandResolver"/> backed by an agent, so a
    /// <c>DrylAiCommandPalette</c> (or <c>DrylCommandPalette Resolver=</c>) resolves natural-language
    /// queries into command tool calls. Supply the agent from your own DI:
    /// <code>builder.Services.AddDrylCommandResolver(sp => sp.GetRequiredService&lt;MyAgent&gt;());</code>
    /// </summary>
    public static IServiceCollection AddDrylCommandResolver(
        this IServiceCollection services,
        Func<IServiceProvider, Microsoft.Agents.AI.AIAgent> agentFactory)
    {
        services.AddScoped<DRYL.Components.ICommandResolver>(
            sp => new DrylAiCommandResolver(agentFactory(sp)));
        return services;
    }
}
