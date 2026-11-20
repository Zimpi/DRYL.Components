namespace DRYL.Components;

/// <summary>
/// Joins a component's own inline declarations with the consumer's <c>Style</c>.
/// The component's come first and the consumer's last, so both apply and the
/// consumer wins for a property both name — the twin of the merged <c>Class</c>.
/// </summary>
internal static class InlineStyle
{
    /// <summary>Returns the merged declaration list, or null when there is nothing to write.</summary>
    public static string? Merge(string? own, string? consumer)
    {
        own = Trim(own);
        consumer = Trim(consumer);
        if (own is null) return consumer;
        if (consumer is null) return own;
        return $"{own}; {consumer}";
    }

    // Drops surrounding whitespace and trailing semicolons, so the join never
    // writes ";;" and an empty value writes no attribute.
    private static string? Trim(string? value)
    {
        var trimmed = value?.Trim().TrimEnd(';', ' ');
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
