using System.Text.Json;

namespace DRYL.Components.Canvas;

/// <summary>A user interaction inside a canvas artifact (button click), carrying the intent
/// and a snapshot of every input value. Feed <see cref="ToPromptMessage"/> to the chat agent.</summary>
public sealed record CanvasInteraction(
    string Intent, string NodeId, IReadOnlyDictionary<string, object?> Values)
{
    /// <summary>
    /// A ready-made chat turn that replaces the generated one. Set by an action's
    /// <see cref="CanvasActionResult.AskAi"/>; <c>null</c> for a plain intent click.
    /// <para>Because <see cref="ToPromptMessage"/> returns it verbatim, a host that already wires
    /// <c>OnInteraction="i => _chat.Send(i.ToPromptMessage())"</c> picks it up unchanged.</para>
    /// </summary>
    public string? Message { get; init; }

    /// <summary>Structured chat message describing this interaction — send it as the next
    /// user turn so the assistant can react (typically with update_artifact).</summary>
    public string ToPromptMessage() =>
        Message ??
        "The user interacted with the artifact. intent: \"" + Intent + "\", values: "
        + JsonSerializer.Serialize(Values, CanvasJson.Options)
        + ". React accordingly; update the artifact via update_artifact if appropriate.";
}
