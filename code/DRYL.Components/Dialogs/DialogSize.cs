namespace DRYL.Components.Dialogs;

/// <summary>
/// Width preset for a <see cref="DRYL.Components.DrylDialog"/>. Maps to the
/// <c>.dialog-sm</c> / <c>.dialog-md</c> / <c>.dialog-lg</c> / <c>.dialog-fullscreen</c>
/// CSS classes in <c>dryl.css</c>.
/// </summary>
public enum DialogSize
{
    /// <summary>Compact dialog (≈ 420px wide) — confirmations, alerts.</summary>
    Small,

    /// <summary>Default dialog (≈ 640px wide) — forms, content.</summary>
    Medium,

    /// <summary>Wide dialog (≈ 960px wide) — rich content, multi-column layouts.</summary>
    Large,

    /// <summary>Fills the viewport — immersive flows.</summary>
    Fullscreen
}
