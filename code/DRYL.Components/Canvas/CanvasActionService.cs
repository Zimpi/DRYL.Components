using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DRYL.Components.Canvas;

/// <summary>
/// The per-scope view onto the registered canvas actions: what exists, and how to run it.
///
/// <para>Its only caller is <see cref="CanvasActionRunner"/>, and that runner is only ever
/// reached from a rendered button's click handler. There is deliberately no path from a model
/// output to here — the AI builds and labels the button, the human presses it.</para>
/// </summary>
public interface ICanvasActionService
{
    /// <summary>Every registered action. Also the material for the model's prompt block.</summary>
    IReadOnlyList<CanvasActionDescriptor> Descriptors { get; }

    /// <summary>Infrastructure — runs a registered action against this scope. A bound canvas
    /// calls it after a user press; hosts do not.</summary>
    Task<CanvasActionResult> InvokeAsync(
        string name, JsonElement? args, string nodeId,
        IReadOnlyDictionary<string, object?> values, CancellationToken ct);
}

/// <inheritdoc cref="ICanvasActionService" />
internal sealed class CanvasActionService : ICanvasActionService
{
    private readonly CanvasActionRegistry _registry;
    private readonly IServiceProvider _scope;

    // The block goes into *every* generation, so a large registry is a standing token cost.
    // Once per process is the right cadence for saying so.
    private static int _crowdedWarned;

    public CanvasActionService(CanvasActionRegistry registry, IServiceProvider scope)
    {
        _registry = registry;
        _scope = scope;

        if (registry.Descriptors.Count >= CanvasActionPrompt.CrowdedAt &&
            Interlocked.Exchange(ref _crowdedWarned, 1) == 0)
        {
            (scope.GetService(typeof(ILogger<CanvasActionService>)) as ILogger)?.LogWarning(
                "{Count} canvas actions are registered. Their descriptors go into every artifact " +
                "generation — keep each description to one line, or the prompt grows with the catalog.",
                registry.Descriptors.Count);
        }
    }

    public IReadOnlyList<CanvasActionDescriptor> Descriptors => _registry.Descriptors;

    public Task<CanvasActionResult> InvokeAsync(
        string name, JsonElement? args, string nodeId,
        IReadOnlyDictionary<string, object?> values, CancellationToken ct)
    {
        if (!_registry.TryGet(name, out var action))
            throw new InvalidOperationException($"No canvas action named '{name}' is registered.");

        return action.Invoke(args, new CanvasActionContext(_scope, nodeId, values), ct);
    }
}
