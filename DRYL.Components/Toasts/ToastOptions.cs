namespace DRYL.Components.Toasts;

/// <summary>
/// Per-call configuration for a toast. Pass to <see cref="IDrylToastService.Show"/> and friends.
/// </summary>
public sealed class ToastOptions
{
    /// <summary>Semantic flavor. Default: <see cref="ToastVariant.Default"/>.</summary>
    public ToastVariant Variant { get; set; } = ToastVariant.Default;

    /// <summary>
    /// Optional toast title — rendered above the message in a stronger weight.
    /// When using <see cref="IDrylToastService.Show{TComponent}"/>, this is the title shown
    /// above the custom component body.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Auto-dismiss duration in milliseconds. <c>0</c> disables auto-dismiss (toast stays
    /// until the user closes it or it is closed programmatically). Default: <c>5000</c>.
    /// </summary>
    public int Duration { get; set; } = 5000;

    /// <summary>If true (default), the toast shows a close (×) button.</summary>
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>
    /// If true (default), hovering the toast pauses the auto-dismiss timer and progress bar.
    /// Ignored when <see cref="Duration"/> is <c>0</c>.
    /// </summary>
    public bool PauseOnHover { get; set; } = true;

    /// <summary>
    /// If true (default), shows a progress bar at the bottom that drains over <see cref="Duration"/>.
    /// Ignored when <see cref="Duration"/> is <c>0</c>.
    /// </summary>
    public bool ShowProgress { get; set; } = true;

    /// <summary>
    /// AI ambient state. When set to anything other than <see cref="AiState.None"/>, the AI
    /// aura (rotating gradient ring + breathing glow) dominates the visual; the
    /// <see cref="Variant"/> accent is preserved on the icon chip only.
    /// </summary>
    public AiState Ai { get; set; } = AiState.None;

    /// <summary>
    /// Optional icon name to override the variant's default icon (see <see cref="DrylIcon"/>).
    /// Use <see cref="string.Empty"/> to suppress the icon entirely.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>Optional extra CSS class applied to the toast container.</summary>
    public string? Class { get; set; }
}
