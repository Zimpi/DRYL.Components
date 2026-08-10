namespace DRYL.Components;

/// <summary>Visual state of an input component. Drives border color and focus glow.</summary>
public enum InputState
{
    /// <summary>Neutral state — default border, accent focus ring.</summary>
    Default,

    /// <summary>Error state — danger-colored border and focus ring. Auto-applied when EditForm validation fails.</summary>
    Error,

    /// <summary>Success state — success-colored border and focus ring. Set manually after a passing check.</summary>
    Success
}
