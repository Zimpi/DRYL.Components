using System.Text;

namespace DRYL.Components.Canvas;

/// <summary>
/// Turns the registered actions into the block an artifact generator sees. The model learns which
/// buttons it may offer — and that pressing them is not its job.
/// </summary>
public static class CanvasActionPrompt
{
    /// <summary>Past this many actions the block starts to dominate every generation; the real
    /// answer is catalog compression, so until then this is a warning, not a limit.</summary>
    internal const int CrowdedAt = 40;

    /// <summary>
    /// The block for <paramref name="descriptors"/>, or an empty string when nothing is registered —
    /// in which case the generator's contract stays exactly as it was and existing chat artifacts
    /// keep using plain <c>intent</c> buttons.
    /// </summary>
    public static string Block(IReadOnlyList<CanvasActionDescriptor>? descriptors)
    {
        if (descriptors is null || descriptors.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.Append("\nACTIONS — wire buttons to these instead of inventing an intent:\n");
        foreach (var d in descriptors)
        {
            sb.Append("- ").Append(d.Name).Append('(').Append(Signature(d)).Append(')');
            if (!string.IsNullOrWhiteSpace(d.Description))
                sb.Append(" — \"").Append(d.Description).Append('"');
            sb.Append('\n');
        }
        sb.Append(
            """
            Wire like this: "action": { "name": "<name>", "args": { … }, "confirm": "<question>"? }
            "action" sits on a button node next to "props" — never inside them.
            An arg is a literal, or { "$field": "<name of an interactive node in this artifact>" } —
            the same reference syntax as a data param.
            A button with an "action" may omit "intent".
            Add "confirm" to anything destructive or irreversible, and set "kind": "danger" on the button.
            You place the button and label it. You NEVER trigger an action — only the user presses it.

            """);
        return sb.ToString();
    }

    private static string Signature(CanvasActionDescriptor d) =>
        string.Join(", ", d.Args.Select(a => $"{a.Name}{(a.Required ? "" : "?")}: {a.TypeName}"));
}
