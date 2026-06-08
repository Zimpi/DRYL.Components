using System;
using System.Collections.Generic;

namespace DRYL.Components;

/// <summary>
/// Service-driven notification inbox for DRYL, patterned after <see cref="Toasts.IDrylToastService"/>.
/// Push entries from anywhere (background jobs, AI completions, SignalR) and a
/// <see cref="DrylNotifications"/> bound to this service renders them.
/// </summary>
/// <remarks>
/// Registered as scoped (one per Blazor circuit) via <c>builder.Services.AddDrylComponents()</c>.
/// A <see cref="DrylNotifications"/> with no <c>Items</c> parameter binds to this service
/// automatically; pass <c>Items</c> instead to drive the inbox from your own state.
/// </remarks>
public interface IDrylNotificationService
{
    /// <summary>All notifications, newest first.</summary>
    IReadOnlyList<DrylNotification> Notifications { get; }

    /// <summary>Number of unread notifications — the bell badge count.</summary>
    int UnreadCount { get; }

    /// <summary>Adds an existing notification (newest first) and returns it.</summary>
    DrylNotification Add(DrylNotification notification);

    /// <summary>Convenience overload that builds and adds a notification.</summary>
    DrylNotification Add(string title, string? message = null, string? icon = null, AiState ai = AiState.None);

    /// <summary>Marks the notification with the given id as read.</summary>
    void MarkRead(string id);

    /// <summary>Marks every notification as read.</summary>
    void MarkAllRead();

    /// <summary>Removes the notification with the given id.</summary>
    void Remove(string id);

    /// <summary>Removes every notification.</summary>
    void Clear();

    /// <summary>Raised whenever the inbox changes so bound components can re-render.</summary>
    event Action? OnChanged;
}
