using System.Text.Json;

namespace DRYL.Components.Canvas;

/// <summary>A user interaction inside a canvas artifact (button click), carrying the intent
/// and a snapshot of every input value. Feed <see cref="ToPromptMessage"/> to the chat agent.</summary>
public sealed record CanvasInteraction(
    string Intent, string NodeId, IReadOnlyDictionary<string, object?> Values)
{
    /// <summary>Structured chat message describing this interaction — send it as the next
    /// user turn so the assistant can react (typically with update_artifact).</summary>
    public string ToPromptMessage() =>
        "The user interacted with the artifact. intent: \"" + Intent + "\", values: "
        + JsonSerializer.Serialize(Values, CanvasJson.Options)
        + ". React accordingly; update the artifact via update_artifact if appropriate.";
}
