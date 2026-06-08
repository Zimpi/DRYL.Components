using System;
using System.Collections.Generic;
using System.Linq;

namespace DRYL.Components;

/// <summary>
/// Default <see cref="IDrylNotificationService"/> implementation. Registered as scoped
/// (one per Blazor circuit) via <c>AddDrylComponents()</c>.
/// </summary>
internal sealed class DrylNotificationService : IDrylNotificationService
{
    // Stored newest-first so the inbox renders the latest entry at the top without re-sorting.
    private readonly List<DrylNotification> _items = new();

    public IReadOnlyList<DrylNotification> Notifications => _items;

    public int UnreadCount => _items.Count(n => !n.Read);

    public DrylNotification Add(DrylNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _items.Insert(0, notification);
        OnChanged?.Invoke();
        return notification;
    }

    public DrylNotification Add(string title, string? message = null, string? icon = null, AiState ai = AiState.None) =>
        Add(new DrylNotification { Title = title, Message = message, Icon = icon, Ai = ai });

    public void MarkRead(string id)
    {
        var item = _items.FirstOrDefault(n => n.Id == id);
        if (item is { Read: false })
        {
            item.Read = true;
            OnChanged?.Invoke();
        }
    }

    public void MarkAllRead()
    {
        var changed = false;
        foreach (var n in _items)
            if (!n.Read) { n.Read = true; changed = true; }
        if (changed) OnChanged?.Invoke();
    }

    public void Remove(string id)
    {
        if (_items.RemoveAll(n => n.Id == id) > 0)
            OnChanged?.Invoke();
    }

    public void Clear()
    {
        if (_items.Count == 0) return;
        _items.Clear();
        OnChanged?.Invoke();
    }

    public event Action? OnChanged;
}
