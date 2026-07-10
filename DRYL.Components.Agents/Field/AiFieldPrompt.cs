using System.Text;

namespace DRYL.Components.Agents;

/// <summary>
/// Builds the English model prompt for a <see cref="DrylAiField"/> invocation and cleans the reply.
/// English template on purpose — models follow the "respond with ONLY the replacement" contract
/// more reliably; the user-facing UI strings stay German.
/// </summary>
internal static class AiFieldPrompt
{
    /// <summary>Composes instruction + optional context + current value + optional selection.</summary>
    internal static string Build(string instruction, string? context, string value, string? selection)
    {
        var sb = new StringBuilder();
        sb.AppendLine(instruction.Trim());

        if (!string.IsNullOrWhiteSpace(context))
        {
            sb.AppendLine();
            sb.AppendLine("Additional context:");
            sb.AppendLine(context.Trim());
        }

        if (!string.IsNullOrEmpty(value))
        {
            sb.AppendLine();
            sb.AppendLine("Current field value:");
            sb.AppendLine("\"\"\"");
            sb.AppendLine(value);
            sb.AppendLine("\"\"\"");
        }

        if (selection is not null)
        {
            sb.AppendLine();
            sb.AppendLine("Selected portion (transform ONLY this):");
            sb.AppendLine("\"\"\"");
            sb.AppendLine(selection);
            sb.AppendLine("\"\"\"");
        }

        sb.AppendLine();
        sb.Append("Respond with ONLY the replacement text for ");
        sb.Append(selection is not null ? "the selected portion" : "the field");
        sb.Append(" — no quotes, no markdown fences, no explanation.");
        return sb.ToString();
    }

    /// <summary>
    /// Defensive post-trim: strips one wrapping pair of double quotes or one fenced code block
    /// if the model added them despite the contract.
    /// </summary>
    internal static string Clean(string raw)
    {
        var text = raw.Trim();

        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var firstBreak = text.IndexOf('\n');
            var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
            if (firstBreak >= 0 && lastFence > firstBreak)
                return text[(firstBreak + 1)..lastFence].Trim();
        }
        else if (text.Length >= 2 && text[0] == '"' && text[^1] == '"')
        {
            return text[1..^1];
        }

        return text;
    }
}
