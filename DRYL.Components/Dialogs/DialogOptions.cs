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
}
