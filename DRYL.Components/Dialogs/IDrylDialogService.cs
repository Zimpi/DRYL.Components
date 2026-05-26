using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace DRYL.Components.Dialogs;

/// <summary>
/// Service-driven dialog API for DRYL. Patterned after <c>MudBlazor.IDialogService</c>:
/// show a typed dialog and await its <see cref="DialogResult"/>.
/// </summary>
/// <remarks>
/// Requires <c>builder.Services.AddDrylComponents()</c> in startup and a single
/// <c>&lt;DrylDialogProvider/&gt;</c> placed in the root layout.
/// </remarks>
public interface IDrylDialogService
{
    /// <summary>
    /// Show a dialog of type <typeparamref name="TDialog"/>.
    /// </summary>
    /// <param name="title">Optional title displayed in the dialog header.</param>
    /// <param name="parameters">Parameters forwarded to the dialog component.</param>
    /// <param name="options">Per-dialog options (size, close behavior, AI state).</param>
    /// <returns>A reference whose <see cref="IDrylDialogReference.Result"/> completes when the dialog closes.</returns>
    Task<IDrylDialogReference> ShowAsync<TDialog>(
        string? title = null,
        DialogParameters? parameters = null,
        DialogOptions? options = null)
        where TDialog : IComponent;

    /// <summary>
    /// Show a confirmation dialog with two buttons (cancel / confirm). The returned
    /// result is <c>Canceled = false</c> if the user confirmed.
    /// </summary>
    Task<DialogResult> ShowConfirmAsync(
        string title,
        string message,
        string confirmLabel = "Confirm",
        string cancelLabel = "Cancel",
        DialogOptions? options = null);

    /// <summary>
    /// Show a simple alert dialog with a single OK button. Always returns <c>Ok</c>.
    /// </summary>
    Task<DialogResult> ShowAlertAsync(
        string title,
        string message,
        string okLabel = "OK",
        DialogOptions? options = null);

    /// <summary>
    /// Fired by the service whenever a new dialog is requested. Subscribed by
    /// <c>DrylDialogProvider</c> to render the dialog.
    /// </summary>
    event Action<IDrylDialogReference>? OnDialogInstanceAdded;

    /// <summary>
    /// Fired when a dialog closes (so the provider can remove it from its render list).
    /// </summary>
    event Action<IDrylDialogReference>? OnDialogCloseRequested;

    /// <summary>
    /// Fired when a dialog's internal state changes (e.g. AI state) and the provider
    /// must re-render.
    /// </summary>
    event Action<IDrylDialogReference>? OnDialogInstanceUpdated;
}
