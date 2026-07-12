namespace DRYL.Components.Ai;

/// <summary>
/// Builds the AI-aura host class list from an <see cref="AuraLifecycle"/> and the resolved
/// <see cref="AiAura"/> variant, so every host emits the exact same <c>ai-aura</c> /
/// <c>ai-aura--aurora</c> / <c>ai-aura--out</c> / state-class combination. Pairs with
/// <c>&lt;DrylAuraElements/&gt;</c>, which renders the matching ring / glow / wash markup.
/// </summary>
public static class AiAuraCss
{
    /// <summary>
    /// Appends the aura classes for the current lifecycle + variant to
    /// <paramref name="classes"/>. Adds nothing while the aura is absent (state None and
    /// not exiting), so a host with AI off keeps its plain class list.
    /// </summary>
    public static void Append(List<string> classes, AuraLifecycle aura, AiAura variant)
    {
        if (!aura.Present) return;

        classes.Add("ai-aura");
        if (variant == AiAura.Aurora) classes.Add("ai-aura--aurora");
        if (aura.Exiting) classes.Add("ai-aura--out");

        switch (aura.RenderState)
        {
            case AiState.Thinking:  classes.Add("ai-thinking");  break;
            case AiState.Streaming: classes.Add("ai-streaming"); break;
            case AiState.Generated: classes.Add("ai-generated"); break;
        }
    }
}
