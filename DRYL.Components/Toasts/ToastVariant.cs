namespace DRYL.Components.Toasts;

/// <summary>
/// Semantic flavor of a toast. Drives icon, accent color and glow.
/// </summary>
/// <remarks>
/// When a toast is also AI-aware (<see cref="ToastOptions.Ai"/> != <see cref="AiState.None"/>),
/// the AI aura dominates visually — the variant fades to a quieter accent on the icon chip only,
/// so a user can still tell at a glance whether the AI delivered success/info/warning/error.
/// </remarks>
public enum ToastVariant
{
    /// <summary>No semantic color — neutral glass surface, no icon chip.</summary>
    Default,

    /// <summary>Confirmation / completion. Green accent + check icon.</summary>
    Success,

    /// <summary>Warning / non-blocking issue. Amber accent + triangle icon.</summary>
    Warning,

    /// <summary>Error / failure. Red accent + alert icon.</summary>
    Error,

    /// <summary>Informational. Cyan accent + info icon.</summary>
    Info
}
