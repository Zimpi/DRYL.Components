namespace DRYL.Components.Dialogs;

/// <summary>
/// Per-call configuration for a dialog. Pass to
/// <see cref="IDrylDialogService.ShowAsync{TDialog}(string?, DialogParameters?, DialogOptions?)"/>.
/// </summary>
public sealed class DialogOptions
{
    /// <summary>Width preset. Default: <see cref="DialogSize.Medium"/>.</summary>
    public DialogSize Size { get; set; } = DialogSize.Medium;

    /// <summary>If true (default), pressing <c>Escape</c> cancels the dialog.</summary>
    public bool CloseOnEscape { get; set; } = true;

    /// <summary>If true (default), clicking the backdrop cancels the dialog.</summary>
    public bool CloseOnBackdropClick { get; set; } = true;

    /// <summary>If true (default), the header shows a close (×) button.</summary>
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>
    /// Initial AI state for the dialog frame. The dialog component itself can override
    /// this via its own <c>Ai</c> parameter or by calling
    /// <see cref="IDrylDialogInstance.SetAi(AiState)"/> at runtime.
    /// </summary>
    public AiState Ai { get; set; } = AiState.None;

    /// <summary>Optional extra CSS class applied to the dialog container.</summary>
    public string? Class { get; set; }

    /// <summary>
    /// When true, a dialog opened while a sibling is still closing (the sequential
    /// "agent handoff" pattern — see the Sequential demo) morphs into the new one via
    /// the browser's morph engine (<see cref="DRYL.Components.Motion.IDrylMorph"/>):
    /// the dialog shell glides to its new size/position while its title, body and
    /// footer cross-fade independently, instead of the default CSS cross-fade
    /// (predecessor plays its exit while the successor enters). Off by default —
    /// opt in per call, and keep it consistent across every step of a chain (a
    /// mismatched flag on one step just falls back to the plain cross-fade for that
    /// step). Falls back automatically in browsers without View Transition support,
    /// during prerender, and when the user prefers reduced motion.
    /// </summary>
    public bool AnimateHandoff { get; set; }

    /// <summary>
    /// Morph tier for the <see cref="AnimateHandoff"/> transition. Defaults to
    /// <see cref="DrylMorphStyle.DepthGlass"/> — a dialog handoff is exactly
    /// the rare, high-meaning merge that tier is for: the mercury-merge + translucency
    /// pulse makes the content swap read as a deliberate change even when the dialog's
    /// size barely moves (e.g. two confirm dialogs of similar length), instead of
    /// looking like a plain text cross-fade. Set to <see cref="DrylMorphStyle.Glide"/>
    /// for the cheaper shape-only morph. Ignored unless <see cref="AnimateHandoff"/> is true.
    /// </summary>
    public DrylMorphStyle HandoffStyle { get; set; } = DrylMorphStyle.DepthGlass;
}
