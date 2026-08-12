using System.Text.Json;
using DRYL.Components.Canvas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DRYL.Components;

/// <summary>
/// DI helpers for registering canvas data sources — the host side of Canvas Data Binding.
/// A node then references a source by name instead of carrying numbers the model wrote:
/// <code>{ "id": "chart", "type": "lineChart",
///   "data": { "source": "sales.byMonth", "params": { "year": 2026 }, "refresh": "interval:30s" } }</code>
/// </summary>
public static class CanvasServiceCollectionExtensions
{
    /// <summary>
    /// Registers a named canvas data source with typed parameters.
    /// <code>
    /// public sealed record SalesParams(int Year, string? Region = null);
    ///
    /// builder.Services.AddDrylCanvasDataSource("sales.byMonth",
    ///     "Umsatz je Monat in Tsd €.",
    ///     async (SalesParams p, CanvasDataContext ctx, CancellationToken ct) =>
    ///     {
    ///         var db = ctx.Services.GetRequiredService&lt;AppDb&gt;();
    ///         var rows = await db.SalesAsync(p.Year, p.Region, ct);
    ///         return CanvasData.Series(rows.Select(r => r.Month), ("Umsatz", rows.Select(r => r.Total)));
    ///     });
    /// </code>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The key used in a node's <c>data.source</c>. Convention: <c>area.thing</c>.</param>
    /// <param name="description">One model-facing sentence: what it returns and in which unit.
    /// Do <em>not</em> describe the parameters — those are derived from <typeparamref name="TParams"/>.</param>
    /// <param name="handler">Runs the query. Tenant and user come from <c>ctx.Services</c>, never from the spec.</param>
    /// <typeparam name="TParams">A record whose primary constructor declares the parameters.</typeparam>
    /// <typeparam name="TData">The result shape — determines which node types may bind to this source.</typeparam>
    /// <exception cref="ArgumentException">A parameter uses an unsupported type.</exception>
    /// <exception cref="InvalidOperationException">The name is already registered.</exception>
    public static IServiceCollection AddDrylCanvasDataSource<TParams, TData>(
        this IServiceCollection services,
        string name,
        string description,
        Func<TParams, CanvasDataContext, CancellationToken, Task<TData>> handler)
        where TParams : class
        where TData : CanvasData
    {
        ArgumentNullException.ThrowIfNull(handler);
        ValidateName(name);

        var descriptor = new CanvasDataDescriptor(
            name, description ?? string.Empty, ShapeOf<TData>(), CanvasParamSchema.Describe(typeof(TParams)));

        Registry(services).Add(new CanvasDataSource(descriptor, async (json, scope, ct) =>
        {
            var p = Deserialize<TParams>(json, name);
            return await handler(p, new CanvasDataContext(scope), ct).ConfigureAwait(false);
        }));

        return services;
    }

    /// <summary>Registers a named canvas data source that takes no parameters.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The key used in a node's <c>data.source</c>. Convention: <c>area.thing</c>.</param>
    /// <param name="description">One model-facing sentence: what it returns and in which unit.</param>
    /// <param name="handler">Runs the query. Tenant and user come from <c>ctx.Services</c>, never from the spec.</param>
    /// <typeparam name="TData">The result shape — determines which node types may bind to this source.</typeparam>
    /// <exception cref="InvalidOperationException">The name is already registered.</exception>
    public static IServiceCollection AddDrylCanvasDataSource<TData>(
        this IServiceCollection services,
        string name,
        string description,
        Func<CanvasDataContext, CancellationToken, Task<TData>> handler)
        where TData : CanvasData
    {
        ArgumentNullException.ThrowIfNull(handler);
        ValidateName(name);

        var descriptor = new CanvasDataDescriptor(
            name, description ?? string.Empty, ShapeOf<TData>(), Array.Empty<CanvasParamInfo>());

        Registry(services).Add(new CanvasDataSource(descriptor, async (_, scope, ct) =>
            await handler(new CanvasDataContext(scope), ct).ConfigureAwait(false)));

        return services;
    }

    /// <summary>
    /// Registers a named canvas action with typed arguments. A button in the artifact binds to it
    /// through <c>"action": { "name": … }</c>; only a user press ever runs it — the AI authors and
    /// labels the button, it never presses it.
    /// <code>
    /// public sealed record ApproveArgs(string OrderId, string? Note = null);
    ///
    /// builder.Services.AddDrylCanvasAction("order.approve",
    ///     "Approves an order.",
    ///     async (ApproveArgs a, CanvasActionContext ctx, CancellationToken ct) =>
    ///     {
    ///         await ctx.Services.GetRequiredService&lt;IOrderService&gt;().ApproveAsync(a.OrderId, ct);
    ///         return CanvasActionResult.Ok("Order approved").Refresh("orders.open");
    ///     });
    /// </code>
    /// </summary>
    /// <remarks>A success message is shown as a toast, which needs a <c>&lt;DrylToastProvider/&gt;</c>
    /// in the layout; a <c>confirm</c> on the binding needs a <c>&lt;DrylDialogProvider/&gt;</c> —
    /// without it the action is refused rather than run unconfirmed.</remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The key used in a node's <c>action.name</c>. Convention: <c>area.thing</c>.</param>
    /// <param name="description">One model-facing sentence: what the action does. Do <em>not</em>
    /// describe the arguments — those are derived from <typeparamref name="TArgs"/>.</param>
    /// <param name="handler">Runs the command. Tenant and user come from <c>ctx.Services</c>, never from the spec.</param>
    /// <typeparam name="TArgs">A record whose primary constructor declares the arguments.</typeparam>
    /// <exception cref="ArgumentException">An argument uses an unsupported type, or the name is empty.</exception>
    /// <exception cref="InvalidOperationException">The name is already registered.</exception>
    public static IServiceCollection AddDrylCanvasAction<TArgs>(
        this IServiceCollection services,
        string name,
        string description,
        Func<TArgs, CanvasActionContext, CancellationToken, Task<CanvasActionResult>> handler)
        where TArgs : class
    {
        ArgumentNullException.ThrowIfNull(handler);
        ValidateName(name);

        var descriptor = new CanvasActionDescriptor(
            name, description ?? string.Empty, CanvasParamSchema.Describe(typeof(TArgs)));

        ActionRegistry(services).Add(new CanvasActionSource(descriptor, async (json, ctx, ct) =>
        {
            var args = Deserialize<TArgs>(json, name);
            return await handler(args, ctx, ct).ConfigureAwait(false);
        }));

        return services;
    }

    /// <summary>Registers a named canvas action that takes no arguments.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The key used in a node's <c>action.name</c>. Convention: <c>area.thing</c>.</param>
    /// <param name="description">One model-facing sentence: what the action does.</param>
    /// <param name="handler">Runs the command. Tenant and user come from <c>ctx.Services</c>, never from the spec.</param>
    /// <exception cref="ArgumentException">The name is empty.</exception>
    /// <exception cref="InvalidOperationException">The name is already registered.</exception>
    public static IServiceCollection AddDrylCanvasAction(
        this IServiceCollection services,
        string name,
        string description,
        Func<CanvasActionContext, CancellationToken, Task<CanvasActionResult>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ValidateName(name);

        var descriptor = new CanvasActionDescriptor(
            name, description ?? string.Empty, Array.Empty<CanvasParamInfo>());

        ActionRegistry(services).Add(new CanvasActionSource(descriptor,
            async (_, ctx, ct) => await handler(ctx, ct).ConfigureAwait(false)));

        return services;
    }

    // The registry is a singleton *instance* so registrations can be collected while the
    // collection is still being built — a source added here must be visible to the very first
    // scope, and resolving the container just to register into it is not an option.
    internal static CanvasDataRegistry Registry(IServiceCollection services)
    {
        foreach (var d in services)
            if (d.ServiceType == typeof(CanvasDataRegistry) && d.ImplementationInstance is CanvasDataRegistry existing)
                return existing;

        var registry = new CanvasDataRegistry();
        services.AddSingleton(registry);
        services.AddScoped<ICanvasDataService>(sp => new CanvasDataService(registry, sp));
        return registry;
    }

    // Same shape as Registry(services), and a separate registry on purpose: reading data and
    // running a command are different privileges, and a name may legitimately exist in both.
    internal static CanvasActionRegistry ActionRegistry(IServiceCollection services)
    {
        foreach (var d in services)
            if (d.ServiceType == typeof(CanvasActionRegistry) &&
                d.ImplementationInstance is CanvasActionRegistry existing)
                return existing;

        var registry = new CanvasActionRegistry();
        services.AddSingleton(registry);
        services.AddScoped<ICanvasActionService>(sp => new CanvasActionService(registry, sp));
        return registry;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A canvas data source or action needs a non-empty name.", nameof(name));
    }

    private static CanvasDataShape ShapeOf<TData>() where TData : CanvasData
    {
        // The shape is static, not inferred from a result at runtime: that is what lets a wrong
        // node type surface as a validation sentence in the receipt rather than as a blank widget.
        var t = typeof(TData);
        if (t == typeof(CanvasScalarData)) return CanvasDataShape.Scalar;
        if (t == typeof(CanvasSeriesData)) return CanvasDataShape.Series;
        if (t == typeof(CanvasSegmentData)) return CanvasDataShape.Segments;
        if (t == typeof(CanvasRowData)) return CanvasDataShape.Rows;

        throw new ArgumentException(
            $"A canvas data source must return one concrete shape (CanvasScalarData, CanvasSeriesData, " +
            $"CanvasSegmentData or CanvasRowData) — '{t.Name}' does not say which. Change the lambda's " +
            "return type, e.g. by returning CanvasData.Series(…) directly.");
    }

    /// <summary>
    /// Registers the in-memory canvas document store, so <c>DrylCanvasWorkspace</c> can save and
    /// load dashboards. Replace it with your own by registering an <see cref="ICanvasDocumentStore"/>
    /// first — this call never overwrites an existing registration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static IServiceCollection AddDrylCanvasDocumentStore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<ICanvasDocumentStore, InMemoryCanvasDocumentStore>();
        return services;
    }

    /// <summary>Registers a host implementation of <see cref="ICanvasDocumentStore"/>.</summary>
    /// <remarks>Pass <see cref="ServiceLifetime.Scoped"/> for a store that scopes its documents to
    /// the signed-in user: reading the user means depending on a scoped service, which a singleton
    /// cannot do.</remarks>
    /// <typeparam name="TStore">The host's store.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The store's lifetime. Default <see cref="ServiceLifetime.Singleton"/>.</param>
    public static IServiceCollection AddDrylCanvasDocumentStore<TStore>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TStore : class, ICanvasDocumentStore
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAdd(new ServiceDescriptor(typeof(ICanvasDocumentStore), typeof(TStore), lifetime));
        return services;
    }

    private static TParams Deserialize<TParams>(JsonElement? json, string source) where TParams : class
    {
        try
        {
            var value = json is { ValueKind: JsonValueKind.Object }
                ? JsonSerializer.Deserialize<TParams>(json.Value.GetRawText(), CanvasJson.Options)
                : JsonSerializer.Deserialize<TParams>("{}", CanvasJson.Options);
            return value ?? throw new InvalidOperationException(
                $"Source '{source}': the params object could not be bound.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Source '{source}': invalid params — {ex.Message}", ex);
        }
    }
}
