using Microsoft.AspNetCore.Components;
using System;

namespace DRYL.Components.Toasts;

/// <summary>
/// Service-driven toast API for DRYL. Patterned after <see cref="IDrylDialogService"/>:
/// call <see cref="Show(string, ToastOptions?)"/> (or one of the typed helpers) and the
/// active <see cref="DRYL.Components.DrylToastProvider"/> renders it.
/// </summary>
/// <remarks>
/// Requires <c>builder.Services.AddDrylComponents()</c> in startup and a single
/// <c>&lt;DrylToastProvider/&gt;</c> placed in the root layout.
/// </remarks>
public interface IDrylToastService
{
    /// <summary>
    /// Show a toast with the given message. <paramref name="options"/> drives the variant,
    /// title, duration, and AI state.
    /// </summary>
    IDrylToastReference Show(string message, ToastOptions? options = null);

    /// <summary>Show a green success toast.</summary>
    IDrylToastReference ShowSuccess(string message, string? title = null, ToastOptions? options = null);

    /// <summary>Show an amber warning toast.</summary>
    IDrylToastReference ShowWarning(string message, string? title = null, ToastOptions? options = null);

    /// <summary>Show a red error toast.</summary>
    IDrylToastReference ShowError(string message, string? title = null, ToastOptions? options = null);

    /// <summary>Show a cyan info toast.</summary>
    IDrylToastReference ShowInfo(string message, string? title = null, ToastOptions? options = null);

    /// <summary>
    /// Show a toast whose body is rendered by <typeparamref name="TComponent"/>. The toast
    /// frame (icon chip, title from <see cref="ToastOptions.Title"/>, close button, progress
    /// bar) is still provided by DRYL; the component fills the body slot.
    /// </summary>
    /// <remarks>
    /// The component receives an <see cref="IDrylToastInstance"/> as a cascading parameter
    /// and can close its own toast by calling <see cref="IDrylToastInstance.Close"/>.
    /// </remarks>
    IDrylToastReference Show<TComponent>(
        ToastParameters? parameters = null,
        ToastOptions? options = null)
        where TComponent : IComponent;

    /// <summary>
    /// Close every visible toast (e.g. on navigation or sign-out).
    /// </summary>
    void CloseAll();

    /// <summary>
    /// Fired by the service whenever a new toast is requested. Subscribed by
    /// <see cref="DRYL.Components.DrylToastProvider"/>.
    /// </summary>
    event Action<IDrylToastReference>? OnToastAdded;

    /// <summary>Fired when a toast is being dismissed (start of exit animation).</summary>
    event Action<IDrylToastReference>? OnToastCloseRequested;

    /// <summary>Fired when a toast's internal state changes (e.g. AI state).</summary>
    event Action<IDrylToastReference>? OnToastUpdated;
}
