using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace DRYL.Components.Dialogs;

/// <summary>
/// Default <see cref="IDrylDialogService"/> implementation. Registered as scoped
/// (one per Blazor circuit) via <c>AddDrylComponents()</c>.
/// </summary>
internal sealed class DrylDialogService : IDrylDialogService
{
    public event Action<IDrylDialogReference>? OnDialogInstanceAdded;
    public event Action<IDrylDialogReference>? OnDialogCloseRequested;
    public event Action<IDrylDialogReference>? OnDialogInstanceUpdated;

    public Task<IDrylDialogReference> ShowAsync<TDialog>(
        string? title = null,
        DialogParameters? parameters = null,
        DialogOptions? options = null)
        where TDialog : IComponent
    {
        var reference = new DrylDialogReference(
            this,
            typeof(TDialog),
            title,
            parameters,
            options ?? new DialogOptions());

        OnDialogInstanceAdded?.Invoke(reference);
        return Task.FromResult<IDrylDialogReference>(reference);
    }

    public async Task<DialogResult> ShowConfirmAsync(
        string title,
        string message,
        string confirmLabel = "Confirm",
        string cancelLabel = "Cancel",
        DialogOptions? options = null)
    {
        var parameters = new DialogParameters
        {
            ["Message"] = message,
            ["ConfirmLabel"] = confirmLabel,
            ["CancelLabel"] = cancelLabel
        };
        var reference = await ShowAsync<DrylConfirmDialog>(
            title,
            parameters,
            options ?? new DialogOptions { Size = DialogSize.Small });
        return await reference.Result;
    }

    public async Task<DialogResult> ShowAlertAsync(
        string title,
        string message,
        string okLabel = "OK",
        DialogOptions? options = null)
    {
        var parameters = new DialogParameters
        {
            ["Message"] = message,
            ["OkLabel"] = okLabel
        };
        var reference = await ShowAsync<DrylAlertDialog>(
            title,
            parameters,
            options ?? new DialogOptions { Size = DialogSize.Small });
        return await reference.Result;
    }

    internal void NotifyClose(IDrylDialogReference reference) =>
        OnDialogCloseRequested?.Invoke(reference);

    internal void NotifyUpdated(IDrylDialogReference reference) =>
        OnDialogInstanceUpdated?.Invoke(reference);
}
