using System;
using System.Threading.Tasks;

namespace DRYL.Components;

/// <summary>
/// Shared layout state passed via <see cref="Microsoft.AspNetCore.Components.CascadingValue{TValue}"/>
/// from <see cref="DrylLayout"/> to its descendants (<see cref="DrylAppBar"/>, <see cref="DrylDrawer"/>).
/// Lets the app bar render a hamburger that toggles the drawer without the consumer
/// wiring state by hand.
/// </summary>
public sealed class DrylLayoutContext
{
    private DrylDrawer? _drawer;

    /// <summary>True when a <see cref="DrylDrawer"/> is mounted inside the layout.</summary>
    public bool HasDrawer => _drawer is not null;

    /// <summary>Current open state of the registered drawer (false if none).</summary>
    public bool DrawerOpen => _drawer?.Open ?? false;

    /// <summary>Raised when the registered drawer's open state changes.</summary>
    public event Action? StateChanged;

    internal void RegisterDrawer(DrylDrawer drawer)
    {
        _drawer = drawer;
        StateChanged?.Invoke();
    }

    internal void UnregisterDrawer(DrylDrawer drawer)
    {
        if (ReferenceEquals(_drawer, drawer))
        {
            _drawer = null;
            StateChanged?.Invoke();
        }
    }

    internal void NotifyStateChanged() => StateChanged?.Invoke();

    // ── Sidebar collapse coordination ──────────────────────────────────────────
    // The DrylDrawer owns its collapsed state (via @bind-Collapsed) and registers a
    // toggler here so a DrylAppBar button — or any consumer — can collapse / expand
    // it without wiring state by hand. DrylLayout reads SidebarCollapsed to reflect
    // the icon-width grid column on the .app-shell.

    private Func<Task>? _sidebarToggler;

    /// <summary>
    /// Current collapsed state of the registered sidebar drawer. <c>false</c> when no
    /// collapsible drawer is mounted. Maintained by <see cref="DrylDrawer"/>.
    /// </summary>
    public bool SidebarCollapsed { get; internal set; }

    /// <summary>True when a collapsible/pinnable <see cref="DrylDrawer"/> has registered a toggler.</summary>
    public bool CanCollapseSidebar => _sidebarToggler is not null;

    internal void RegisterSidebarToggle(Func<Task> toggler)
    {
        _sidebarToggler = toggler;
        StateChanged?.Invoke();
    }

    internal void UnregisterSidebarToggle(Func<Task> toggler)
    {
        if (ReferenceEquals(_sidebarToggler, toggler))
        {
            _sidebarToggler = null;
            SidebarCollapsed = false;
            StateChanged?.Invoke();
        }
    }

    /// <summary>Collapses or expands the registered collapsible sidebar. No-op when none is mounted.</summary>
    public Task ToggleSidebarAsync() => _sidebarToggler?.Invoke() ?? Task.CompletedTask;

    /// <summary>Toggles the registered drawer. No-op when none is mounted.</summary>
    public Task ToggleDrawerAsync() => _drawer?.ToggleAsync() ?? Task.CompletedTask;

    /// <summary>Closes the registered drawer. No-op when none is mounted.</summary>
    public Task CloseDrawerAsync() => _drawer?.SetOpenAsync(false) ?? Task.CompletedTask;

    /// <summary>Opens the registered drawer. No-op when none is mounted.</summary>
    public Task OpenDrawerAsync() => _drawer?.SetOpenAsync(true) ?? Task.CompletedTask;
}
