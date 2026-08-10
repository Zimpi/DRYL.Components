using Microsoft.Extensions.AI;

namespace DRYL.Components.Agents;

/// <summary>
/// Accumulated token usage of an agent run, summed from every <see cref="UsageContent"/> the
/// update stream delivers. Exposed as <see cref="DrylRunBase.Usage"/> (null until the first
/// usage update arrives — many providers only report usage on the final chunk, some not at
/// all). Render it with <c>DrylAgentUsage</c>.
/// </summary>
public sealed class DrylRunUsage
{
    /// <summary>Prompt-side (input) tokens, if the provider reported them.</summary>
    public long? InputTokens { get; private set; }

    /// <summary>Completion-side (output) tokens, if the provider reported them.</summary>
    public long? OutputTokens { get; private set; }

    /// <summary>Total tokens, if the provider reported them.</summary>
    public long? TotalTokens { get; private set; }

    internal void Add(UsageDetails details)
    {
        if (details.InputTokenCount is { } input) InputTokens = (InputTokens ?? 0) + input;
        if (details.OutputTokenCount is { } output) OutputTokens = (OutputTokens ?? 0) + output;
        if (details.TotalTokenCount is { } total) TotalTokens = (TotalTokens ?? 0) + total;
    }

    internal void Add(DrylRunUsage other)
    {
        if (other.InputTokens is { } input) InputTokens = (InputTokens ?? 0) + input;
        if (other.OutputTokens is { } output) OutputTokens = (OutputTokens ?? 0) + output;
        if (other.TotalTokens is { } total) TotalTokens = (TotalTokens ?? 0) + total;
    }
}
