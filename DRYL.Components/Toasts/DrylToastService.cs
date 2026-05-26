using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;

namespace DRYL.Components.Toasts;

/// <summary>
/// Default <see cref="IDrylToastService"/> implementation. Registered as scoped
/// (one per Blazor circuit) via <c>AddDrylComponents()</c>.
/// </summary>
internal sealed class DrylToastService : IDrylToastService
{
    private readonly List<DrylToastReference> _active = new();

    public event Action<IDrylToastReference>? OnToastAdded;
    public event Action<IDrylToastReference>? OnToastCloseRequested;
    public event Action<IDrylToastReference>? OnToastUpdated;

    public IDrylToastReference Show(string message, ToastOptions? options = null)
    {
        var reference = new DrylToastReference(this, options ?? new ToastOptions(), message);
        _active.Add(reference);
        OnToastAdded?.Invoke(reference);
        return reference;
    }

    public IDrylToastReference ShowSuccess(string message, string? title = null, ToastOptions? options = null) =>
        ShowVariant(ToastVariant.Success, message, title, options);

    public IDrylToastReference ShowWarning(string message, string? title = null, ToastOptions? options = null) =>
        ShowVariant(ToastVariant.Warning, message, title, options);

    public IDrylToastReference ShowError(string message, string? title = null, ToastOptions? options = null) =>
        ShowVariant(ToastVariant.Error, message, title, options);

    public IDrylToastReference ShowInfo(string message, string? title = null, ToastOptions? options = null) =>
        ShowVariant(ToastVariant.Info, message, title, options);

    public IDrylToastReference Show<TComponent>(
        ToastParameters? parameters = null,
        ToastOptions? options = null)
        where TComponent : IComponent
    {
        var reference = new DrylToastReference(
            this,
            options ?? new ToastOptions(),
            message: null,
            bodyType: typeof(TComponent),
            bodyParameters: parameters);
        _active.Add(reference);
        OnToastAdded?.Invoke(reference);
        return reference;
    }

    public void CloseAll()
    {
        foreach (var t in _active.ToArray())
        {
            t.Close();
        }
    }

    private IDrylToastReference ShowVariant(
        ToastVariant variant,
        string message,
        string? title,
        ToastOptions? options)
    {
        var opts = options ?? new ToastOptions();
        opts.Variant = variant;
        if (title is not null) opts.Title = title;
        return Show(message, opts);
    }

    internal void NotifyClose(DrylToastReference reference)
    {
        OnToastCloseRequested?.Invoke(reference);
    }

    internal void NotifyRemoved(DrylToastReference reference)
    {
        _active.Remove(reference);
        reference.RaiseClosed();
    }

    internal void NotifyUpdated(DrylToastReference reference)
    {
        OnToastUpdated?.Invoke(reference);
    }
}
