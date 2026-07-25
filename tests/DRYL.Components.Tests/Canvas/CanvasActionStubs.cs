using DRYL.Components.Dialogs;
using DRYL.Components.Toasts;
using Microsoft.AspNetCore.Components;

namespace DRYL.Components.Tests.Canvas;

/// <summary>Confirms or declines every confirmation dialog, and records what it was asked.</summary>
internal sealed class StubDialogService : IDrylDialogService
{
    public bool Confirm { get; set; } = true;

    /// <summary>Completes the confirmation asynchronously, the way a real dialog does — the user
    /// takes a moment. A synchronously-completing stub hides every bug that lives in the
    /// continuation after the await (see the dispatcher note on CanvasActionRunner).</summary>
    public bool Yield { get; set; }

    public List<(string Title, string Message)> Asked { get; } = new();

    public Task<IDrylDialogReference> ShowAsync<TDialog>(
        string? title = null, DialogParameters? parameters = null, DialogOptions? options = null)
        where TDialog : IComponent => throw new NotSupportedException();

    public async Task<DialogResult> ShowConfirmAsync(string title, string message,
        string confirmLabel = "Confirm", string cancelLabel = "Cancel", DialogOptions? options = null)
    {
        Asked.Add((title, message));
        if (Yield) await Task.Yield();
        return Confirm ? DialogResult.Ok() : DialogResult.Cancel();
    }

    public Task<DialogResult> ShowAlertAsync(string title, string message,
        string okLabel = "OK", DialogOptions? options = null) => Task.FromResult(DialogResult.Ok());

#pragma warning disable CS0067 // the runner never subscribes; the provider does
    public event Action<IDrylDialogReference>? OnDialogInstanceAdded;
    public event Action<IDrylDialogReference>? OnDialogCloseRequested;
    public event Action<IDrylDialogReference>? OnDialogInstanceUpdated;
#pragma warning restore CS0067
}

/// <summary>Records the toasts a canvas action asked for.</summary>
internal sealed class StubToastService : IDrylToastService
{
    public List<string> Successes { get; } = new();
    public List<string> Errors { get; } = new();

    public IDrylToastReference Show(string message, ToastOptions? options = null) => new StubToastReference();

    public IDrylToastReference ShowSuccess(string message, string? title = null, ToastOptions? options = null)
    {
        Successes.Add(message);
        return new StubToastReference();
    }

    public IDrylToastReference ShowWarning(string message, string? title = null, ToastOptions? options = null) =>
        new StubToastReference();

    public IDrylToastReference ShowError(string message, string? title = null, ToastOptions? options = null)
    {
        Errors.Add(message);
        return new StubToastReference();
    }

    public IDrylToastReference ShowInfo(string message, string? title = null, ToastOptions? options = null) =>
        new StubToastReference();

    public IDrylToastReference Show<TComponent>(ToastParameters? parameters = null, ToastOptions? options = null)
        where TComponent : IComponent => new StubToastReference();

    public void CloseAll() { }

#pragma warning disable CS0067 // the runner never subscribes; the provider does
    public event Action<IDrylToastReference>? OnToastAdded;
    public event Action<IDrylToastReference>? OnToastCloseRequested;
    public event Action<IDrylToastReference>? OnToastUpdated;
#pragma warning restore CS0067

    private sealed class StubToastReference : IDrylToastReference
    {
        public Guid Id { get; } = Guid.NewGuid();
        public void Close() { }
        public void SetAi(AiState state) { }
#pragma warning disable CS0067
        public event Action<IDrylToastReference>? OnClosed;
#pragma warning restore CS0067
    }
}
